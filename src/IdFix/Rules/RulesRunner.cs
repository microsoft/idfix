using IdFix.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Windows.Forms;

namespace IdFix.Rules
{
    class RulesRunnerDoWorkArgs { }

    #region RulesRunnerResult

    /// <summary>
    /// Defines the result of executing this rules runner
    /// </summary>
    public class RulesRunnerResult : Dictionary<string, RuleCollectionResult>
    {
        private IEnumerable<ComposedRuleResult> _dataSet = null;

        public new void Add(string key, RuleCollectionResult result)
        {
            this._dataSet = null;
            base.Add(key, result);
        }

        public TimeSpan TotalElapsed
        {
            get
            {
                return this.Select(pair => pair.Value.Elapsed).Aggregate(new TimeSpan(0), (collector, next) => collector.Add(next));
            }
        }

        public long TotalErrors
        {
            get
            {
                return this.Select(pair => pair.Value.TotalErrors).Aggregate((long)0, (collector, next) => collector + next);
            }
        }

        public long TotalDuplicates
        {
            get
            {
                return this.Select(pair => pair.Value.TotalDuplicates).Aggregate((long)0, (collector, next) => collector + next);
            }
        }

        public long TotalSkips
        {
            get
            {
                return this.Select(pair => pair.Value.TotalSkips).Aggregate((long)0, (collector, next) => collector + next);
            }
        }

        public long TotalFound
        {
            get
            {
                return this.Select(pair => pair.Value.TotalFound).Aggregate((long)0, (collector, next) => collector + next);
            }
        }

        public long TotalProcessed
        {
            get
            {
                return this.Select(pair => pair.Value.TotalProcessed).Aggregate((long)0, (collector, next) => collector + next);
            }
        }

        /// <summary>
        /// Produces a set of all results from each attribute rule executed against each entity ordered by distinguised name
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ComposedRuleResult> ToDataset()
        {
            if (this._dataSet == null)
            {
                // this 
                this._dataSet = this.Select(r => r.Value).Select(v => v.Errors).Aggregate(new List<ComposedRuleResult>(), (list, next) =>
                {
                    list.AddRange(next);
                    return list;
                });
            }

            return this._dataSet.OrderBy(s => s.EntityDistinguishedName, StringComparer.CurrentCultureIgnoreCase);
        }
    }

    #endregion

    /// <summary>
    /// A <see cref="BackgroundWorker"/> that runs one or more RuleCollections against one or more connections based on settings
    /// </summary>
    class RulesRunner : BackgroundWorker
    {
        /// <summary>
        /// Creates a new instance of the <see cref="RulesRunner"/> class
        /// </summary>
        public RulesRunner()
        {
            this.WorkerSupportsCancellation = true;
            this.DoWork += this.Run;
        }

        /// <summary>
        /// Event to bubble status update messages to the containing form
        /// </summary>
        public event OnStatusUpdateDelegate OnStatusUpdate;

        #region Run

        /// <summary>
        /// Runs the rules
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">args</param>
        private void Run(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (!(e.Argument is RulesRunnerDoWorkArgs args))
                {
                    e.Result = null;
                    throw new ArgumentException("RulesRunner expects arguments of type RulesRunnerDoWorkArgs.");
                }

                var stopwatch = DateTime.Now;

                // clear out our duplicate tracking each run
                DuplicateStore.Reset();

                // create the connection manager and bubble up any messages
                var connections = new ConnectionManager();
                connections.OnStatusUpdate += (string message) => { this.OnStatusUpdate?.Invoke(message); };

                // used to collect the results as we process rule collections
                var results = new RulesRunnerResult();

                connections.WithConnections((LdapConnection connection, string distinguishedName) =>
                {
                    if (this.CancellationPending)
                    {
                        e.Result = null;
                        return;
                    }

                    // we try and create a key from the queried directory and fail to random guid
                    var servers = ((LdapDirectoryIdentifier)connection.Directory).Servers;
                    var identifier = servers.Length > 0 ? servers.First() : Guid.NewGuid().ToString("D");

                    // get the rule collection to run
                    var ruleCollection = this.GetRuleCollection(distinguishedName);

                    // if connecting to Global Catalog Server, make sure the collection's analyzed attributes are replicated.
                    if (SettingsManager.Instance.Port == Constants.GlobalCatalogPort)
                    {
                        var schemaResult = this.ExecuteRuleCollectionSchemaCheck(ruleCollection, connection);

                        // handle the cancel case
                        if (schemaResult == null && this.CancellationPending)
                        {
                            e.Result = null;
                            return;
                        }

                        // display errors
                        if (schemaResult.Count > 0)
                        {
                            DialogResult dialogResult = DialogResult.None;

                            // Show message box on main thread.
                            FormApp.Instance.Invoke(new Action(() =>
                            {
                                var message = string.Format(StringLiterals.SchemaWarningMessage, Environment.NewLine + string.Join(Environment.NewLine, schemaResult.ToArray()));
                                dialogResult = MessageBox.Show(FormApp.Instance, message, StringLiterals.SchemaWarningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            }));

                            if (dialogResult == DialogResult.No)
                            {
                                return;
                            }
                        }
                    }

                    // runs the rule collection
                    var result = this.ExecuteRuleCollection(ruleCollection, connection);

                    // handle the cancel case
                    if (result == null && this.CancellationPending)
                    {
                        e.Result = null;
                        return;
                    }

                    // add our result to the list
                    results.Add(identifier, result);
                });

                // we pass back the collection of all results for processing into the UI grid
                e.Result = results;
            }
            catch (Exception err)
            {
                this.OnStatusUpdate?.Invoke("Error in RulesRunner: " + err.Message);
                e.Result = err;
            }
            finally
            {
                DuplicateStore.Reset();
            }
        }

        #endregion

        #region GetSchemaDistinguishedName

        private string GetSchemaDistinguishedName(LdapConnection connection)
        {
            var request = new SearchRequest(null, "(objectClass=*)", SearchScope.Base, Constants.SchemaNamingContextAttribute);

            var response = (SearchResponse)connection.SendRequest(request);

            return response.Entries[0].Attributes[Constants.SchemaNamingContextAttribute][0].ToString();
        }

        #endregion

        #region ExecuteRuleCollectionSchemaCheck

        private List<string> ExecuteRuleCollectionSchemaCheck(RuleCollection collection, LdapConnection connection)
        {
            this.OnStatusUpdate?.Invoke(StringLiterals.LdapConnectionEstablishing);
            var schemaDistinguishedName = this.GetSchemaDistinguishedName(connection);
            using (var searcher = collection.CreateSchemaSearcher(schemaDistinguishedName))
            {
                this.OnStatusUpdate?.Invoke(StringLiterals.LdapConnectionEstablished);
                this.OnStatusUpdate?.Invoke(StringLiterals.BeginningQuery);

                if (this.CancellationPending)
                {
                    return null;
                }

                var replicatedAttributes = new List<string>();
                var results = searcher.FindAll();

                foreach (System.DirectoryServices.SearchResult entry in results)
                {
                    if (entry.Properties.Contains(Constants.IsMemberOfPartialAttributeSetAttribute)
                     && entry.Properties[Constants.IsMemberOfPartialAttributeSetAttribute].Count > 0
                     && (bool)entry.Properties[Constants.IsMemberOfPartialAttributeSetAttribute][0] == true)
                    {
                        replicatedAttributes.Add(entry.Properties[Constants.LdapDisplayNameAttribute][0].ToString());
                    }
                }

                var notReplicatedAttributes = collection.AttributesToQuery.Where(_ => !replicatedAttributes.Contains(_, StringComparer.InvariantCultureIgnoreCase)).ToList();

                return notReplicatedAttributes;
            }
        }

        #endregion

        #region GetRuleCollection

        /// <summary>
        /// Gets the correct rule collection, MultiTenant or Dedicated, based on the current application settings
        /// </summary>
        /// <param name="connection">LdapConnection used to make the requests</param>
        /// <param name="distinguishedName">Distinguised name calculated while constructing the connection</param>
        /// <returns>A rule collection to run against the supplied connection</returns>
        private RuleCollection GetRuleCollection(string distinguishedName)
        {
            RuleCollection collection;
            if (SettingsManager.Instance.CurrentRuleMode == RuleMode.MultiTenant)
            {
                collection = new MultiTenantRuleCollection(distinguishedName);
            }
            else
            {
                collection = new DedicatedRuleCollection(distinguishedName);
            }

            collection.OnStatusUpdate += (string message) => { this.OnStatusUpdate?.Invoke(message); };

            return collection;
        }

        #endregion

        #region ExecuteRuleCollection

        /// <summary>
        /// Executes a rule collection against the supplied connection
        /// </summary>
        /// <param name="collection">The rule collection to execute</param>
        /// <param name="connection">The connection used to retrieve entries for processing</param>
        /// <returns><see cref="RuleCollectionResult"/> or null if canceled</returns>
        private RuleCollectionResult ExecuteRuleCollection(RuleCollection collection, LdapConnection connection)
        {
            // these count all the totals for the connection against which this RuleCollection is being run
            var stopWatch = new Stopwatch();
            long skipCount = 0;
            long entryCount = 0;
            long duplicateCount = 0;
            long errorCount = 0;

            this.OnStatusUpdate?.Invoke(StringLiterals.LdapConnectionEstablishing);
            var searchRequest = collection.CreateSearchRequest();
            this.OnStatusUpdate?.Invoke(StringLiterals.LdapConnectionEstablished);

            var errors = new List<ComposedRuleResult>();

            this.OnStatusUpdate?.Invoke(StringLiterals.BeginningQuery);

            while (true)
            {
                var searchResponse = (SearchResponse)connection.SendRequest(searchRequest);

                // verify support for paged results
                if (searchResponse.Controls.Length != 1 || !(searchResponse.Controls[0] is PageResultResponseControl))
                {
                    this.OnStatusUpdate?.Invoke(StringLiterals.CannotPageResultSet);
                    throw new InvalidOperationException(StringLiterals.CannotPageResultSet);
                }

                foreach (SearchResultEntry entry in searchResponse.Entries)
                {
                    if (this.CancellationPending)
                    {
                        return null;
                    }

                    if (collection.Skip(entry))
                    {
                        skipCount++;
                        continue;
                    }

                    // this tracks the number of entries we have processed and not skipped
                    entryCount++;

                    foreach (var composedRule in collection.Rules)
                    {
                        // run each composed rule which can produce multiple results
                        var results = composedRule.Execute(entry);

                        for (var i = 0; i < results.Length; i++)
                        {
                            var result = results[i];
                            if (!result.Success)
                            {
                                errorCount++;

                                if (result.Results.Any(r => (r.ErrorTypeFlags & ErrorType.Duplicate) != 0))
                                {
                                    duplicateCount++;

                                    if (result.ProposedAction == ActionType.Edit)
                                    {
                                        try
                                        {
                                            // Add original LDAP entry with the same value.
                                            var originalEntry = DuplicateStore.GetOriginalSearchResultEntry(result.AttributeName, result.OriginalValue);
                                            var additionalResult = new ComposedRuleResult
                                            {
                                                AttributeName = result.AttributeName,
                                                EntityDistinguishedName = originalEntry.DistinguishedName,
                                                EntityCommonName = originalEntry.Attributes[StringLiterals.Cn][0].ToString(),
                                                ObjectType = ComposedRule.GetObjectType(entry),
                                                OriginalValue = result.OriginalValue,
                                                ProposedAction = result.ProposedAction,
                                                ProposedValue = result.ProposedValue,
                                                Results = new RuleResult[] {
                                                    new RuleResult(false)
                                                    {
                                                        ErrorTypeFlags = ErrorType.Duplicate,
                                                        ProposedAction = result.ProposedAction,
                                                        UpdatedValue = result.OriginalValue
                                                    }
                                                },
                                                Success = result.Success
                                            };
                                            errors.Add(additionalResult);
                                        }
                                        catch
                                        {
                                            // Suppress exception which may occur when adding the other matching duplicate entry.
                                        }
                                    }
                                }

                                errors.Add(result);
                            }
                        }
                    }
                }

                // handle paging
                var cookie = searchResponse.Controls.OfType<PageResultResponseControl>().First().Cookie;

                // if this is true, there are no more pages to request
                if (cookie.Length == 0)
                    break;

                searchRequest.Controls.OfType<PageResultRequestControl>().First().Cookie = cookie;
            }

            // we are all done, stop tracking time
            stopWatch.Stop();

            return new RuleCollectionResult
            {
                TotalDuplicates = duplicateCount,
                TotalErrors = errorCount,
                TotalFound = skipCount + entryCount,
                TotalSkips = skipCount,
                TotalProcessed = entryCount,
                Elapsed = stopWatch.Elapsed,
                Errors = errors.ToArray()
            };
        }

        #endregion
    }
}

using IdFix.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices.Protocols;
using System.Linq;

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

            this.OnStatusUpdate?.Invoke("Please wait while the LDAP Connection is established.");
            var searchRequest = collection.CreateSearchRequest();
            this.OnStatusUpdate?.Invoke("LDAP Connection established.");

            var errors = new List<ComposedRuleResult>();

            this.OnStatusUpdate?.Invoke("Beginning query");

            while (true)
            {
                var searchResponse = (SearchResponse)connection.SendRequest(searchRequest);

                // verify support for paged results
                if (searchResponse.Controls.Length != 1 || !(searchResponse.Controls[0] is PageResultResponseControl))
                {
                    this.OnStatusUpdate?.Invoke("The server cannot page the result set.");
                    throw new InvalidOperationException("The server cannot page the result set.");
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
                            if (!results[i].Success)
                            {
                                errorCount++;

                                if (results[i].Results.Any(r => (r.ErrorTypeFlags & ErrorType.Duplicate) != 0))
                                {
                                    duplicateCount++;
                                }

                                errors.Add(results[i]);
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

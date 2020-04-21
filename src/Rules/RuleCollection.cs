using IdFix.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.Protocols;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdFix.Rules
{
    class RuleCollectionError
    {
        string DistinguisedName { get; set; }
        string ObjectClass { get; set; }
        string AttributeName { get; set; }
        string Error { get; set; }
        string OriginalValue { get; set; }
        string UpdatedValue { get; set; }
    }

    public class RuleCollectionResult
    {
        /// <summary>
        /// Total number of entities found and processed (skipped, errors, success)
        /// </summary>
        public long TotalFound { get; set; }

        /// <summary>
        /// The number of entities skipped
        /// </summary>
        public long TotalSkips { get; set; }

        /// <summary>
        /// The number of entities processed through rules (not skipped)
        /// </summary>
        public long TotalProcessed { get; set; }

        /// <summary>
        /// The number of entities found to have errors (not the total number of errors as an entity could contain multiple errors)
        /// </summary>
        public long TotalErrors { get; set; }

        /// <summary>
        /// The number of entities found to have duplicates
        /// </summary>
        public long TotalDuplicates { get; set; }

        /// <summary>
        /// Records the time spent running the rules against the connection
        /// </summary>
        public TimeSpan Elapsed { get; set; }

        /// <summary>
        /// The set of all errors found when running rules against the entities
        /// </summary>
        public ComposedRuleResult[] Errors { get; set; }
    }

    abstract class RuleCollection
    {
        protected RuleCollection(LdapConnection connection, string distinguishedName, int pageSize = 1000)
        {
            this.Connection = connection;
            this.DistinguishedName = distinguishedName;
            this.PageSize = pageSize;
        }

        public event OnStatusUpdateDelegate OnStatusUpdate;

        protected LdapConnection Connection { get; private set; }
        protected string DistinguishedName { get; private set; }
        protected int PageSize { get; private set; }

        public abstract string[] AttributesToQuery { get; }
        public abstract bool Skip(SearchResultEntry entry);
        public abstract IComposedRule[] Rules { get; }

        public virtual RuleCollectionResult Run()
        {
            // these count all the totals for the connection against which this RuleCollection is being run
            var stopWatch = new Stopwatch();
            long skipCount = 0;
            long entryCount = 0;
            long duplicateCount = 0;
            long errorCount = 0;

            this.OnStatusUpdate?.Invoke("Please wait while the LDAP Connection is established.");
            var searchRequest = this.CreateSearchRequest();

            var errors = new List<ComposedRuleResult>();

            while (true)
            {
                var searchResponse = (SearchResponse)this.Connection.SendRequest(searchRequest);

                // verify support for paged results
                if (searchResponse.Controls.Length != 1 || !(searchResponse.Controls[0] is PageResultResponseControl))
                {
                    this.InvokeStatus("The server cannot page the result set.");
                    throw new InvalidOperationException("The server cannot page the result set.");
                }

                foreach (SearchResultEntry entry in searchResponse.Entries)
                {
                    // TODO:: check for cancel - need to figure out how this works with background worker
                    //if (backgroundWorker1.CancellationPending)
                    //{
                    //    e.Cancel = true;
                    //    e.Result = StringLiterals.CancelQuery;
                    //    files.DeleteByType(FileTypes.Error);
                    //    files.DeleteByType(FileTypes.Duplicate);
                    //    return;
                    //}

                    if (this.Skip(entry))
                    {
                        skipCount++;
                        continue;
                    }

                    // this tracks the number of entries we have processed and not skipped
                    entryCount++;

                    foreach (var composedRule in this.Rules)
                    {
                        // run each composed rule 
                        var result = composedRule.Execute(entry);

                        if (!result.Success)
                        {
                            errorCount++;

                            if (result.Results.Any(r => (r.ErrorTypeFlags & ErrorType.Duplicate) != 0))
                            {
                                duplicateCount++;
                            }

                            errors.Add(result);
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

        #region CreateSearchRequest

        /// <summary>
        /// Creates the search request for this rule collection
        /// </summary>
        /// <returns>Configured search request</returns>
        protected virtual SearchRequest CreateSearchRequest(bool includePaging = true)
        {
            var searchRequest = new SearchRequest(
                this.DistinguishedName,
                SettingsManager.Instance.Filter,
                SearchScope.Subtree,
                this.AttributesToQuery);

            if (includePaging)
            {
                searchRequest.Controls.Add(new PageResultRequestControl(this.PageSize));
            }

            return searchRequest;
        }

        #endregion

        #region InvokeStatus

        /// <summary>
        /// Invokes the OnStatusUpdate event with the supplied message
        /// </summary>
        /// <param name="message">Message to send</param>
        protected virtual void InvokeStatus(string message)
        {
            this.OnStatusUpdate?.Invoke(message);
        }

        #endregion
    }
}

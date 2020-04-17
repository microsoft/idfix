using IdFix.Settings;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdFix.Rules
{
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

        public virtual void Run()
        {
            long entryCount = 0;
            long errorCount = 0;
            long duplicateCount = 0;
            long displayCount = 0;

            this.OnStatusUpdate?.Invoke("Please wait while the LDAP Connection is established.");
            var searchRequest = this.CreateSearchRequest();

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
                        continue;
                    }

                    entryCount++;

                    foreach (var composedRule in this.Rules)
                    {
                        // this needs to do reporting and output, etc
                        var result = composedRule.Execute(entry);

                        if (!result.Success)
                        {
                            // this.ReportError(entry, result);
                        }
                    }

                }

                this.OnStatusUpdate?.Invoke("Query Count: " + entryCount.ToString(CultureInfo.CurrentCulture)
            + "  Error Count: " + errorCount.ToString(CultureInfo.CurrentCulture)
            + "  Duplicate Check Count: " + duplicateCount.ToString(CultureInfo.CurrentCulture));



                // handle paging
                var cookie = searchResponse.Controls.OfType<PageResultResponseControl>().First().Cookie;

                // if this is true, there are no more pages to request
                if (cookie.Length == 0)
                    break;

                searchRequest.Controls.OfType<PageResultRequestControl>().First().Cookie = cookie;
            }
        }

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

        /// <summary>
        /// Invokes the OnStatusUpdate event with the supplied message
        /// </summary>
        /// <param name="message">Message to send</param>
        protected virtual void InvokeStatus(string message)
        {
            this.OnStatusUpdate?.Invoke(message);
        }
    }
}

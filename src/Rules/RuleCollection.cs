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
    class RuleCollectionError
    {
        string DistinguisedName { get; set; }
        string ObjectClass { get; set; }
        string AttributeName { get; set; }
        string Error { get; set; }
        string OriginalValue { get; set; }
        string UpdatedValue { get; set; }
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

        public virtual List<ComposedRuleResult> Run()
        {
            long entryCount = 0;
            long errorCount = 0;
            long duplicateCount = 0;
            long displayCount = 0;

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
                        continue;
                    }

                    entryCount++;

                    foreach (var composedRule in this.Rules)
                    {
                        // this needs to do reporting and output, etc
                        var result = composedRule.Execute(entry);

                        if (!result.Success)
                        {
                            // TODO:: transform these error results into a fuller error object with entity information and other details required for reporting
                            // in the end this collection should be bound to the grid
                            /*
                             * dataGridView1.Rows[newRow].Cells[StringLiterals.DistinguishedName].Value = errorPair.Value.distinguishedName;
                            dataGridView1.Rows[newRow].Cells[StringLiterals.ObjectClass].Value = errorPair.Value.objectClass;
                            dataGridView1.Rows[newRow].Cells[StringLiterals.Attribute].Value = errorPair.Value.attribute;
                            dataGridView1.Rows[newRow].Cells[StringLiterals.Error].Value = errorPair.Value.type.Substring(0, errorPair.Value.type.Length - 1);
                            dataGridView1.Rows[newRow].Cells[StringLiterals.Value].Value = errorPair.Value.value;
                            dataGridView1.Rows[newRow].Cells[StringLiterals.Update].Value = errorPair.Value.update;
                             */
                            errors.Add(result);
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

            return errors;
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

using IdFix.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.Protocols;
using System.Linq;

namespace IdFix.Rules
{
    class RulesRunnerDoWorkArgs { }

    #region RulesRunnerResult

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

    class RulesRunner : BackgroundWorker
    {
        public RulesRunner()
        {
            this.WorkerSupportsCancellation = true;
            this.DoWork += this.RunRules;
        }

        public event OnStatusUpdateDelegate OnStatusUpdate;

        private void RunRules(object sender, DoWorkEventArgs e)
        {
            try
            {
                var args = e.Argument as RulesRunnerDoWorkArgs;
                if (args == null)
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
                    var ruleCollection = this.GetRuleCollection(connection, distinguishedName);

                    // run that collection and add the results into the collection
                    results.Add(identifier, ruleCollection.Run());
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

        #region GetRuleCollection

        /// <summary>
        /// Gets the correct rule collection, MultiTenant or Dedicated, based on the current application settings
        /// </summary>
        /// <param name="connection">LdapConnection used to make the requests</param>
        /// <param name="distinguishedName">Distinguised name calculated while constructing the connection</param>
        /// <returns>A rule collection to run against the supplied connection</returns>
        private RuleCollection GetRuleCollection(LdapConnection connection, string distinguishedName)
        {
            RuleCollection collection;
            if (SettingsManager.Instance.CurrentRuleMode == RuleMode.MultiTenant)
            {
                collection = new MultiTenantRuleCollection(connection, distinguishedName);
            }
            else
            {
                collection = new DedicatedRuleCollection(connection, distinguishedName);
            }

            collection.OnStatusUpdate += (string message) => { this.OnStatusUpdate?.Invoke(message); };

            return collection;
        }

        #endregion
    }
}

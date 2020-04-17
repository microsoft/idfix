using IdFix.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdFix.Rules
{
    class RulesRunnerDoWorkArgs
    {
        public Files Files { get; set; }
    }

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

                //var entryCount = 0;
                //var errorCount = 0;
                //var duplicateCount = 0;
                var stopwatch = DateTime.Now;
                //var errDict.Clear();
                //var dupDict.Clear();
                //var dupObjDict.Clear();

                // clear out our duplicate tracking each run
                DuplicateStore.Reset();
                args.Files.DeleteAll();

                // create the connection manager and bubble up any messages
                var connections = new ConnectionManager();
                connections.OnStatusUpdate += (string message) => { this.OnStatusUpdate?.Invoke(message); };

                var allErrors = new List<ComposedRuleResult>();

                connections.WithConnections((LdapConnection connection, string distinguishedName) =>
                {
                    var ruleCollection = this.GetRuleCollection(connection, distinguishedName);

                    // here we get all the errors returned by this rulecollection
                    // need a compound object
                    // elapsed time
                    // counts
                    // errors collection
                    // more
                    allErrors.AddRange(ruleCollection.Run());
                });

                e.Result = allErrors;
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

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
            this.DoWork += this.RunRules;
            this.RunWorkerCompleted += this.Completed;
        }

        public event OnStatusUpdateDelegate OnStatusUpdate;

        private void RunRules(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as RulesRunnerDoWorkArgs;
            if (args == null)
            {
                e.Result = null;
                throw new ArgumentException("RulesRunner expects arguments of type RulesRunnerDoWorkArgs.");
            }

            var entryCount = 0;
            var errorCount = 0;
            var duplicateCount = 0;
            var stopwatch = DateTime.Now;
            //var errDict.Clear();
            //var dupDict.Clear();
            //var dupObjDict.Clear();
            e.Result = StringLiterals.Complete;

            args.Files.DeleteAll();

            // create the connection manager and bubble up any messages
            var connections = new ConnectionManager();
            connections.OnStatusUpdate += (string message) => { this.OnStatusUpdate?.Invoke(message); };

            connections.WithConnections((LdapConnection connection, string distinguishedName) =>
            {
                var ruleCollection = this.GetRuleCollection(connection, distinguishedName);

                // TODO:: this needs to do some sort of reporting or output, etc
                ruleCollection.Run();




            });
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

        private void Completed(object sender, RunWorkerCompletedEventArgs e)
        {

        }
    }
}

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
        public Files files { get; set; }
        public Action<string> updateStatus { get; set; }
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

            args.files.DeleteAll();

            // create the conneciton manager and bubble up any messages
            var connections = new ConnectionManager();
            connections.OnStatusUpdate += (string message) => { this.OnStatusUpdate?.Invoke(message); };

            connections.WithConnections((LdapConnection connection, string distinguishedName) =>
            {
                int pageSize = 1000;
                var displayCount = 0;

                // here is where we need to call a specific type of searcher
                // because we begin with the attributes defined within that processor
                // MT or Dedicated





                PageResultRequestControl pageRequest = new PageResultRequestControl(pageSize);
                SearchRequest searchRequest = new SearchRequest(
                    distinguishedName,
                    SettingsManager.Instance.Filter,
                    SearchScope.Subtree,
                    new string[] { "nothing" }); // this is the attributes to return
                searchRequest.Controls.Add(pageRequest);
                SearchResponse searchResponse;

                this.OnStatusUpdate?.Invoke("Please wait while the LDAP Connection is established.");











            });
        }

        private void Completed(object sender, RunWorkerCompletedEventArgs e)
        {

        }
    }
}

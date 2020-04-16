using IdFix.Settings;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IdFix.Rules
{
    class ConnectionManager
    {
        /// <summary>
        /// Creates a connection for each entry in the ActiveForestList and executes the provided action passing that connection.
        /// Also calculates the distinguishedName based on business logic
        /// </summary>
        /// <param name="action"></param>
        public void WithConnections(Action<LdapConnection, string> action)
        {
            var activeForestsCopy = SettingsManager.Instance.ActiveForestList.ToArray();
            for (var i = 0; i < activeForestsCopy.Length; i++)
            {
                var activeForest = activeForestsCopy[i];

                string serverName = SettingsManager.Instance.Server;
                string distinguishedName = SettingsManager.Instance.DistinguishedName;

                if (SettingsManager.Instance.CurrentDirectoryType == DirectoryType.ActiveDirectory)
                {
                    if (SettingsManager.Instance.Port == 3268)
                    {
                        serverName = Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, activeForest)).Name;
                        distinguishedName = String.IsNullOrEmpty(SettingsManager.Instance.SearchBase) ? string.Empty : SettingsManager.Instance.SearchBase;
                    }
                    else
                    {
                        serverName = Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, activeForest)).Domains[0].FindDomainController().Name;
                        //targetSearch = "dc=" + Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, forestList[forestListIndex])).Name.Replace(".", ",dc=");
                        if (!String.IsNullOrEmpty(SettingsManager.Instance.SearchBase))
                            distinguishedName = SettingsManager.Instance.SearchBase;
                        else
                            distinguishedName = "dc=" + Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, activeForest)).Name.Replace(".", ",dc=");
                    }
                }
                else
                {
                    if (String.IsNullOrEmpty(SettingsManager.Instance.DistinguishedName))
                    {
                        distinguishedName = "dc=" + Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, activeForest)).Name.Replace(".", ",dc=");
                    }
                }

                using (LdapConnection connection = new LdapConnection(serverName + ":" + SettingsManager.Instance.Port))
                {
                    if (SettingsManager.Instance.Port == 636)
                    {
                        connection.SessionOptions.ProtocolVersion = 3;
                        connection.SessionOptions.SecureSocketLayer = true;
                        connection.AuthType = AuthType.Negotiate;
                    }

                    if (SettingsManager.Instance.CurrentCredentialMode == CredentialMode.Specified)
                    {
                        NetworkCredential credential = new NetworkCredential(SettingsManager.Instance.Username, SettingsManager.Instance.Password);
                        connection.Credential = credential;
                    }

                    connection.Timeout = TimeSpan.FromSeconds(120);
                    this.OnStatusUpdate?.Invoke("RULES:" + SettingsManager.Instance.CurrentRuleMode.ToString() + " SERVER:" + serverName + " PORT:" + SettingsManager.Instance.Port + " FILTER:" + SettingsManager.Instance.Filter);

                    action(connection, distinguishedName);
                }
            }
        }

        public event OnStatusUpdateDelegate OnStatusUpdate;
    }
}
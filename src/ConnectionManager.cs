﻿using IdFix.Settings;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;

namespace IdFix.Rules
{
    class ConnectionManager
    {
        /// <summary>
        /// Creates a connection for each entry in the ActiveForestList and executes the provided action passing that connection as param 1
        /// Also calculates the distinguishedName based on business logic, passed to the action as param 2
        /// </summary>
        /// <param name="action"></param>
        public void WithConnections(Action<LdapConnection, string> action)
        {
            var activeForestsCopy = SettingsManager.Instance.ActiveForestList.ToArray();
            if (activeForestsCopy.Length < 1)
            {
                throw new Exception("No active forests selected. Check the settings and ensure at least one forest is selected.");
            }

            for (var i = 0; i < activeForestsCopy.Length; i++)
            {
                var activeForest = activeForestsCopy[i];

                string serverName = SettingsManager.Instance.Server;
                string distinguishedName = SettingsManager.Instance.DistinguishedName;

                if (SettingsManager.Instance.CurrentDirectoryType == DirectoryType.ActiveDirectory)
                {
                    if (SettingsManager.Instance.Port == Constants.GlobalCatalogPort)
                    {
                        serverName = Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, activeForest)).Name;
                        distinguishedName = SettingsManager.Instance.SearchBaseEnabled && !String.IsNullOrEmpty(SettingsManager.Instance.SearchBase) ? SettingsManager.Instance.SearchBase : string.Empty;
                    }
                    else
                    {
                        serverName = Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, activeForest)).Domains[0].FindDomainController().Name;
                        if (SettingsManager.Instance.SearchBaseEnabled && !String.IsNullOrEmpty(SettingsManager.Instance.SearchBase))
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

                using (var connection = this.CreateConnection(serverName + ":" + SettingsManager.Instance.Port))
                {
                    this.OnStatusUpdate?.Invoke("RULES:" + SettingsManager.Instance.CurrentRuleMode.ToString() + " SERVER:" + serverName + " PORT:" + SettingsManager.Instance.Port + " FILTER:" + SettingsManager.Instance.Filter);

                    action(connection, distinguishedName);
                }
            }
        }

        public event OnStatusUpdateDelegate OnStatusUpdate;

        /// <summary>
        /// Creates an LdapConnection using the given server using application settings
        /// </summary>
        /// <param name="server"></param>
        /// <returns>LdapConnection</returns>
        public LdapConnection CreateConnection(string server)
        {
            var connection = new LdapConnection(server);

            if (SettingsManager.Instance.Port == Constants.LdapSslPort)
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

            return connection;
        }
    }
}
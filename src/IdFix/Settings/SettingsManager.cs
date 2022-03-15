using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.Protocols;

namespace IdFix.Settings
{
    /// <summary>
    /// Used to centrally track application runtime settings
    /// </summary>
    /// <remarks>Settings are managed via FormSettings and read throughout the application from this central store</remarks>
    class SettingsManager
    {
        private static SettingsManager _instance = null;

        public static readonly string DefaultMTFilter = "(|(objectCategory=Person)(objectCategory=Group))";
        public static readonly string DefaultDedicatedFilter = "(&(mail=*)(|(objectCategory=Person)(objectCategory=Group)))";

        private SettingsManager()
        {
            // set defaults
            this.Port = Constants.GlobalCatalogPort;
            this.Filter = SettingsManager.DefaultMTFilter;
            this.SearchBaseEnabled = false;
            this.CurrentDirectoryType = DirectoryType.ActiveDirectory;
            this.CurrentCredentialMode = CredentialMode.CurrentUser;
            this.CurrentRuleMode = RuleMode.MultiTenant;
            this.UseAlternateLogin = false;
            this.AuthType = AuthType.Negotiate;

            // grab our default forests
            var forests = new List<string>();
            forests.Add(Forest.GetCurrentForest().Name);
            this.ForestList = forests.ToArray();
            this.ActiveForestList = forests.ToArray();

            // set search defaults
            this.DistinguishedName = "dc=" + Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, Forest.GetCurrentForest().Name)).Name.Replace(".", ",dc=");
            this.Server = System.Environment.MachineName;
        }

        /// <summary>
        /// Gets the singleton instance containing the application settings
        /// </summary>
        public static SettingsManager Instance
        {
            get
            {
                if (SettingsManager._instance == null)
                {
                    SettingsManager._instance = new SettingsManager();
                }
                return SettingsManager._instance;
            }
        }

        /// <summary>
        /// Gets or sets the list of all forests found by the application
        /// </summary>
        public string[] ForestList { get; set; }

        /// <summary>
        /// Gets or sets the list of active forests selected for scanning
        /// </summary>
        public string[] ActiveForestList { get; set; }

        /// <summary>
        ///  Filter applied to the LDAP query
        /// </summary>
        public string Filter { get; set; }

        /// <summary>
        ///  Port used for the LDAP query
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Indicates the type of directory to scan AD or LDAP
        /// </summary>
        public DirectoryType CurrentDirectoryType { get; set; }

        /// <summary>
        /// For DirectoryType LDAP specifies the distinguished name used in the SearchRequest
        /// </summary>
        public string DistinguishedName { get; set; }

        /// <summary>
        /// For DirectoryType LDAP specifies the search server
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Specifies the curreny active ruleset, Dedicated or MT
        /// </summary>
        public RuleMode CurrentRuleMode { get; set; }

        /// <summary>
        /// Indicates if the search base should be used as part of the query
        /// </summary>
        public bool SearchBaseEnabled { get; set; }

        /// <summary>
        /// Search base used as part of the query
        /// </summary>
        public string SearchBase { get; set; }

        /// <summary>
        /// Ignores UPN Errors
        /// </summary>
        public bool UseAlternateLogin { get; set; }

        /// <summary>
        /// The current credential mode (current user or specified user/pass)
        /// </summary>
        public CredentialMode CurrentCredentialMode { get; set; }

        /// <summary>
        /// Specified username used in non-current user mode
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Specified password used in non-current user mode
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// AuthType to use when connecting to AD
        /// </summary>
        public AuthType AuthType { get; set; }
    }
}

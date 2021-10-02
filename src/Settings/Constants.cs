using System.Text.RegularExpressions;

namespace IdFix.Settings
{
    /// <summary>
    /// Defines constants used in this application
    /// </summary>
    static class Constants
    {
        public static string[] WellKnownExclusions = {
            "Admini",
            "CAS_{",
            "DiscoverySearchMailbox",
            "FederatedEmail",
            "Guest",
            "HTTPConnector",
            "krbtgt",
            "iusr_",
            "iwam",
            "msol",
            "support_",
            "SystemMailbox",
            "WWIOadmini",
            "HealthMailbox",
            "Exchange Online-ApplicationAccount"
        };

        public static Regex DoubleQuotesRegex = new Regex("^[\"]+|[\"]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        public static Regex SMTPRegex = new Regex("smtp:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        public static Regex PeriodsRegex = new Regex(@"^((?!^\.|\.$).)*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        public static Regex DomainPartRegex = new Regex(@"@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+(?:[a-z]{2,17})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        public static Regex LocalPartRegex = new Regex(@"^[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        public static Regex Rfc2822Regex = new Regex(@"^[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+(?:[a-z]{2,17})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        public static Regex InvalidUpnRegExRegex = new Regex(@"[\s\\%&*+/=?'{}|<>\(\)\;\:\,\[\]""äëïöüÿÄËÏÖÜŸ]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        public static Regex InvalidProxyAddressSMTPRegex = new Regex(@"[\s<>\(\)\;\,\[\]""']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        public static Regex InvalidProxyAddressRegex = new Regex(@"[\s<>\(\)\,\[\]""']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        public static Regex InvalidTargetAddressSMTPRegEx = new Regex(@"[\s\\<>\(\)\;\,\[\]""']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        public static Regex InvalidTargetAddressRegEx = new Regex(@"[\s\\<>\(\)\,\[\]""']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        
        // source: https://www.ietf.org/rfc/rfc2253.txt
        public static Regex InvalidX400ProxyAddressRegex = new Regex(@"[\f\n\r\t\v\[\]""']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        public static Regex InvalidX500ProxyAddressRegex = new Regex(@"[\f\n\r\t\v\[\]""']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static int GlobalCatalogPort = 3268;
        public static int LdapPort = 389;
        public static int LdapSslPort = 636;

        public static string SchemaNamingContextAttribute = "schemaNamingContext";
        public static string LdapDisplayNameAttribute = "lDAPDisplayName";
        public static string IsMemberOfPartialAttributeSetAttribute = "isMemberOfPartialAttributeSet";
    }
}



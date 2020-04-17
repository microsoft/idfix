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
    class DedicatedRuleCollection : RuleCollection
    {
        public DedicatedRuleCollection(LdapConnection connection, string distinguishedName, int pageSize = 1000)
            : base(connection, distinguishedName)
        {
        }

        public override IComposedRule[] Rules => throw new NotImplementedException();

        #region AttributesToQuery

        public override string[] AttributesToQuery => new string[] {
            StringLiterals.Cn,
            StringLiterals.DisplayName,
            StringLiterals.DistinguishedName,
            StringLiterals.GivenName,
            StringLiterals.HomeMdb,
            StringLiterals.Mail,
            StringLiterals.MailNickName,
            StringLiterals.ObjectClass,
            StringLiterals.ProxyAddresses,
            StringLiterals.SamAccountName,
            StringLiterals.Sn,
            StringLiterals.TargetAddress
        };

        #endregion

        #region Skip

        public override bool Skip(SearchResultEntry entry)
        {
            string objectDn = String.Empty;
            try
            {
                objectDn = entry.Attributes[StringLiterals.DistinguishedName][0].ToString();

                // Any object is filtered if:
                // match well known exclusion pattern
                if (entry.Attributes[StringLiterals.Cn][0].ToString().EndsWith("$", StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }

                foreach (string exclusion in Constants.WellKnownExclusions)
                {
                    if (entry.Attributes[StringLiterals.Cn][0].ToString().ToUpperInvariant().StartsWith(exclusion.ToUpperInvariant(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }
                }

                // •Object is a conflict object (DN contains \0ACNF: )
                if (entry.Attributes[StringLiterals.DistinguishedName][0].ToString().IndexOf("\0ACNF:", StringComparison.CurrentCultureIgnoreCase) != -1)
                {
                    return true;
                }

                //  •isCriticalSystemObject is present
                if (entry.Attributes.Contains(StringLiterals.IsCriticalSystemObject))
                {
                    if (Convert.ToBoolean(entry.Attributes[StringLiterals.IsCriticalSystemObject][0].ToString(), CultureInfo.CurrentCulture) == true)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                this.InvokeStatus(StringLiterals.Exception + "Result Filter: " + objectDn + "  " + ex.Message);
            }

            return false;
        }

        #endregion
    }
}

using IdFix.Rules.Dedicated;
using IdFix.Rules.Shared;
using IdFix.Settings;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace IdFix.Rules
{
    /// <summary>
    /// Represents the collection of rules to run for dedicated mode
    /// </summary>
    class DedicatedRuleCollection : RuleCollection
    {
        /// <summary>
        /// Holds all the compound rules in this collection
        /// </summary>
        private IComposedRule[] _rules;

        /// <summary>
        /// Creates a new instance of the <see cref="DedicatedRuleCollection"/> class
        /// </summary>
        /// <param name="connection">Configured <see cref="LdapConnection"/> used to make queries</param>
        /// <param name="distinguishedName"></param>
        /// <param name="pageSize"></param>
        public DedicatedRuleCollection(string distinguishedName)
            : base(distinguishedName)
        {
        }

        /// <summary>
        /// Defines the set of rules for this rule collection
        /// </summary>
        public override IComposedRule[] Rules
        {
            get
            {
                if (this._rules == null)
                {
                    /*
                     * ** NOTE **
                     * Based on the original design it matters the order rules are added into a composed rule because the eventual proposed value
                     * is passed through the chain of rules and potentially could be updated multiple times. The original order of the code is
                     * preserved here for each composed rule
                     * 
                     * */

                    var rules = new List<IComposedRule>
                    {
                        new ComposedRule(StringLiterals.DisplayName,
                            new StringMaxLengthRule(256),
                            new RegexRule(new Regex(@"^[\s]+|[\s]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
                            new DisplayNameBlankRule()
                        ),

                        new ComposedRule(StringLiterals.Mail,
                            new StringMaxLengthRule(256),
                            new RegexRule(new Regex(@"[\s]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
                            new RFC2822Rule(),
                            new NoDuplicatesRule()
                        ),

                        new ComposedRule(StringLiterals.MailNickName,
                            new StringMaxLengthRule(64),
                            new RegexRule(new Regex(@"[\s\\!#$%&*+/=?^`{}|~<>()'\;\:\,\[\]""@]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
                            new NoDuplicatesRule(),
                            new MailNicknameBlankRule(),
                            new MailNickNamePeriodsRule()
                        ),

                        new ProxyAddressComposedRule(
                            new StringMaxLengthRule(256),
                            new FixProxyTargetAddressRule(),
                            new NoDuplicatesRule()
                        ),

                        new TargetAddressComposedRule(
                            new StringMaxLengthRule(256),
                            new TargetAddressBlankRule()
                        )
                    };

                    this._rules = rules.ToArray();
                }

                return this._rules;
            }
        }

        #region AttributesToQuery

        /// <summary>
        /// Set of attributes to return for this collection's queries
        /// </summary>
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

        /// <summary>
        /// Logic to determine if a given <see cref="SearchResultEntry"/> is skipped for processing
        /// </summary>
        /// <param name="entry"><see cref="SearchResultEntry"/> to check</param>
        /// <returns>True if <paramref name="entry"/> should be skipped, false if it should be processed</returns>
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

                var exclusionValue = entry.Attributes[StringLiterals.Cn][0].ToString().ToUpperInvariant();
                if (Constants.WellKnownExclusions.Select(e => e.ToUpperInvariant()).Any(e => exclusionValue.StartsWith(e, StringComparison.CurrentCultureIgnoreCase)))
                {
                    return true;
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

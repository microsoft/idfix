using IdFix.Rules.Dedicated;
using IdFix.Rules.Shared;
using IdFix.Settings;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IdFix.Rules
{
    class DedicatedRuleCollection : RuleCollection
    {
        private IComposedRule[] _rules;

        public DedicatedRuleCollection(LdapConnection connection, string distinguishedName, int pageSize = 1000)
            : base(connection, distinguishedName)
        {
        }

        //TODO:: document original order of checks as that matters for behavior

        public override IComposedRule[] Rules
        {
            get
            {
                if (this._rules == null)
                {
                    var rules = new List<IComposedRule>();

                    rules.Add(new ComposedRule(StringLiterals.DisplayName,
                        new StringMaxLengthRule(256),
                        new RegexRule(new Regex(@"^[\s]+|[\s]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
                        new DisplayNameBlankRule()
                    ));

                    rules.Add(new ComposedRule(StringLiterals.Mail,
                        new StringMaxLengthRule(256),
                        new RegexRule(new Regex(@"[\s]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
                        new RFC2822Rule(),
                        new NoDuplicatesRule()
                    ));

                    rules.Add(new ComposedRule(StringLiterals.MailNickName,
                        new StringMaxLengthRule(64),
                        new RegexRule(new Regex(@"[\s\\!#$%&*+/=?^`{}|~<>()'\;\:\,\[\]""@]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
                        new NoDuplicatesRule(),
                        new MailNicknameBlankRule(),
                        new MailNickNamePeriodsRule()
                    ));

                    rules.Add(new ProxyAddressComposedRule(
                        new StringMaxLengthRule(256),
                        new FixProxyTargetAddressRule(),
                        new NoDuplicatesRule()
                    ));

                    rules.Add(new TargetAddressComposedRule(
                        new StringMaxLengthRule(256),
                        new BlankStringRule((entry, value) =>
                        {

                            var objectType = entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString();
                            // the orginal code doesn't provide a fix if the type isn't "contact"
                            if (objectType.Equals("contact", StringComparison.CurrentCultureIgnoreCase))
                            {
                                value = "SMTP:" + entry.Attributes[StringLiterals.Mail][0].ToString();
                                return value.Length > 256 ? value.Substring(0, 256) : value;
                            }

                            return value;
                        })
                    ));

                    this._rules = rules.ToArray();
                }

                return this._rules;
            }
        }

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

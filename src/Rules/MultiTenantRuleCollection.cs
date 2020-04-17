using IdFix.Rules.MultiTentant;
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
    class MultiTenantRuleCollection : RuleCollection
    {
        private IComposedRule[] _rules;


        public MultiTenantRuleCollection(LdapConnection connection, string distinguishedName, int pageSize = 1000)
            : base(connection, distinguishedName)
        {
            this._rules = null;
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
                        new StringMaxLengthRule(255),
                        new BlankStringRule((entry, value) => entry.Attributes[StringLiterals.Cn][0].ToString())));

                    rules.Add(new ComposedRule(StringLiterals.GivenName,
                        new StringMaxLengthRule(63)));

                    rules.Add(new ComposedRule(StringLiterals.Mail,
                        new StringMaxLengthRule(256)
                    // duplicate rule here 
                    ));

                    rules.Add(new ComposedRule(StringLiterals.MailNickName,
                        new StringMaxLengthRule(64),
                        new RegexRule(new Regex(@"^[.]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    // duplicate rule here 
                    ));

                    rules.Add(new ProxyAddressComposedRule(new StringMaxLengthRule(256)
                    // duplicate rule here 
                    ));

                    /**
                    * 
                    * if (entry.Attributes.Contains(StringLiterals.SamAccountName) && !IsValidUpn(entry))
                    {
                        mtChecks(entry, StringLiterals.SamAccountName, 0,
                            objectType.Equals("user", StringComparison.CurrentCultureIgnoreCase) ? 20 : 256,
                            new Regex(@"[\\""|,/\[\]:<>+=;?*]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), null,
                            entry.Attributes.Contains(StringLiterals.UserPrincipalName) ? false : true, false, false);
                    }
                    * */
                    rules.Add(new ComposedRule(StringLiterals.SamAccountName,
                        new SamAccountNameMaxLengthRule(),
                        new RegexRule(new Regex(@"[\\""|,/\[\]:<>+=;?*]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    // duplicate rule here based on type as above
                    ));

                    rules.Add(new TargetAddressComposedRule(
                        new StringMaxLengthRule(255)
                    // duplicate rule here
                    ));

                    if (!SettingsManager.Instance.UseAlternateLogin)
                    {
                        rules.Add(new ComposedRule(StringLiterals.UserPrincipalName,
                            new StringMaxLengthRule(113),
                            new RegexRule(Constants.InvalidUpnRegExRegex),
                            new RFC2822Rule()
                           // duplicate rule here
                           ));
                    }

                    this._rules = rules.ToArray();
                }

                return this._rules;
            }
        }

        #region AttributesToQuery

        public override string[] AttributesToQuery => new string[] {
            StringLiterals.Cn,
            StringLiterals.DistinguishedName,
            StringLiterals.GroupType,
            StringLiterals.HomeMdb,
            StringLiterals.IsCriticalSystemObject,
            StringLiterals.MailNickName,
            StringLiterals.MsExchHideFromAddressLists,
            StringLiterals.MsExchRecipientTypeDetails,
            StringLiterals.ObjectClass,
            StringLiterals.ProxyAddresses,
            StringLiterals.SamAccountName,
            StringLiterals.TargetAddress,
            StringLiterals.UserPrincipalName
        };

        #endregion

        #region Skip

        public override bool Skip(SearchResultEntry entry)
        {
            string objectDn = String.Empty;
            try
            {
                objectDn = entry.Attributes[StringLiterals.DistinguishedName][0].ToString();

                // Active Directory Synchronization in Office 365
                // http://technet.microsoft.com/en-us/library/hh852469.aspx#bkmk_adcleanup
                // Remove duplicate proxyAddress and userPrincipalName attributes.
                // Update blank and invalid userPrincipalName attributes with a valid userPrincipalName.
                // Remove invalid and questionable characters in the givenName, surname (sn), sAMAccountName, displayName, 
                // mail, proxyAddresses, mailNickname, and userPrincipalName attributes. 

                // Appendix F Directory Object Preparation
                // http://technet.microsoft.com/en-us/library/hh852533.aspx

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

                // determine objectClass
                string objectType = entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString();

                // User objects are filtered if:
                if (objectType.Equals("user", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (entry.Attributes.Contains(StringLiterals.SamAccountName))
                    {
                        // SamAccountName exclusions
                        if (entry.Attributes[StringLiterals.SamAccountName][0].ToString().ToUpperInvariant().StartsWith("CAS_", StringComparison.CurrentCultureIgnoreCase)
                            || entry.Attributes[StringLiterals.SamAccountName][0].ToString().ToUpperInvariant().StartsWith("SUPPORT_", StringComparison.CurrentCultureIgnoreCase)
                            || entry.Attributes[StringLiterals.SamAccountName][0].ToString().ToUpperInvariant().StartsWith("MSOL_", StringComparison.CurrentCultureIgnoreCase))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        //  •sAMAccountName is not present
                        //return true;
                    }

                    if (entry.Attributes.Contains(StringLiterals.MailNickName))
                    {
                        // mailNickName exclusions
                        if (entry.Attributes[StringLiterals.MailNickName][0].ToString().ToUpperInvariant().StartsWith("CAS_", StringComparison.CurrentCultureIgnoreCase)
                            || entry.Attributes[StringLiterals.MailNickName][0].ToString().ToUpperInvariant().StartsWith("SYSTEMMAILBOX", StringComparison.CurrentCultureIgnoreCase))
                        {
                            return true;
                        }
                    }

                    if (entry.Attributes.Contains(StringLiterals.DisplayName)
                        && entry.Attributes.Contains(StringLiterals.MsExchHideFromAddressLists))
                    {
                        if (Convert.ToBoolean(entry.Attributes[StringLiterals.MsExchHideFromAddressLists][0].ToString(), CultureInfo.CurrentCulture) == true
                            && entry.Attributes[StringLiterals.DisplayName][0].ToString().ToUpperInvariant().StartsWith("MSOL", StringComparison.CurrentCultureIgnoreCase))
                        {
                            return true;
                        }
                    }

                    //  •msExchRecipientTypeDetails == (0x1000 OR 0x2000 OR 0x4000 OR 0x400000 OR 0x800000 OR 0x1000000 OR 0x20000000)
                    if (entry.Attributes.Contains(StringLiterals.MsExchRecipientTypeDetails))
                    {
                        switch (entry.Attributes[StringLiterals.MsExchRecipientTypeDetails][0].ToString())
                        {
                            case "0x1000":
                                return true;
                            case "0x2000":
                                return true;
                            case "0x4000":
                                return true;
                            case "0x400000":
                                return true;
                            case "0x800000":
                                return true;
                            case "0x1000000":
                                return true;
                            case "0x20000000":
                                return true;
                        }
                    }
                }

                // Group objects are filter if:
                // List of attributes that are synchronized to Office 365 and attributes that are written back to the on-premises Active Directory Domain Services
                // http://support.microsoft.com/kb/2256198
                // NOTE:  these are listed as group filters in the article, but they appear to be an inaccurate mix of filters and errors.
                // SecurityEnabledGroup objects are filtered if: 
                //  •isCriticalSystemObject = TRUE
                //  •mail is present AND DisplayName is not present
                //  •Group has more than 15,000 immediate members
                // MailEnabledGroup objects are filtered if: 
                //  •DisplayName is empty
                //  •(ProxyAddress does not have a primary SMTP address) AND (mail attribute is not present/invalid - i.e. indexof ('@') <= 0)
                //  •Group has more than 15,000 immediate members

                if (objectType.Equals("group", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (Convert.ToInt64(entry.Attributes[StringLiterals.GroupType][0].ToString(), CultureInfo.CurrentCulture) < 0)
                    // security group
                    {
                        if (!entry.Attributes.Contains(StringLiterals.Mail))
                        {
                            return true;
                        }
                    }
                    else
                    // distribution group
                    {
                        if (!entry.Attributes.Contains(StringLiterals.ProxyAddresses))
                        {
                            return true;
                        }
                    }
                }
                return false;
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

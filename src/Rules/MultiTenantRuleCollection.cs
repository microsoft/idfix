using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Globalization;
using System.Text.RegularExpressions;
using IdFix.Rules.MultiTentant;
using IdFix.Rules.Shared;
using IdFix.Settings;

namespace IdFix.Rules
{
    /// <summary>
    /// Represents the collection of rules to run for multi-tenant mode
    /// </summary>
    class MultiTenantRuleCollection : RuleCollection
    {
        /// <summary>
        /// Holds all the compound rules in this collection
        /// </summary>
        private IComposedRule[] _rules;

        /// <summary>
        /// Creates a new instance of the <see cref="MultiTenantRuleCollection"/> class
        /// </summary>
        /// <param name="connection">Configured <see cref="LdapConnection"/> used to make queries</param>
        /// <param name="distinguishedName"></param>
        /// <param name="pageSize"></param>
        public MultiTenantRuleCollection(LdapConnection connection, string distinguishedName)
            : base(connection, distinguishedName)
        {
            this._rules = null;
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

                        // Additional check for DisplayName. 
                        //  Value is non-blank if present
                        //  Max Length = 255. 
                        // The attribute being present check is done in mtChecks, however, the errorBlank check is done only if the attribute
                        // is missing, so that has been updated to check for blanks if attribute present.
                        new ComposedRule(StringLiterals.DisplayName,
                            new StringMaxLengthRule(255),
                            new BlankStringRule((entry, value) => entry.Attributes[StringLiterals.Cn][0].ToString())
                        ),

                        // Additional check for GivenName. 
                        //  Max Length = 63.
                        new ComposedRule(StringLiterals.GivenName,
                            new StringMaxLengthRule(63)
                        ),

                        // New documentation doesn't say anything about mail not being whitespace nor rfc822 format, so pulling that out.
                        // It should just be unique.
                        //  Max Length = 256 -- See XL sheet
                        new ComposedRule(StringLiterals.Mail,
                            new StringMaxLengthRule(256),
                            new NoDuplicatesRule()
                        ),

                        // Updated check for MailNickName
                        //  Cannot start with period (.)
                        //  Max Length = 64, document doesn't restrict, schema says 64
                        new ComposedRule(StringLiterals.MailNickName,
                            new StringMaxLengthRule(64),
                            new RegexRule(new Regex(@"^[.]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
                            new NoDuplicatesRule()
                        ),

                        // ProxyAddresses have additional requirements. 
                        //  Cannot contain space < > () ; , [ ] 
                        //  There should be no ":" in the suffix part of ProxyAddresses.
                        //  SMTP addresses should conform to valid email formats
                        //
                        // Considered and discarded. 
                        // One option is that we do a format check for ProxyAddresses & TargetAddresses to be
                        // <prefix>:<suffix>. In which case, we could pass in RegEx for the else part of smtp.IsMatch()
                        // Instead, we'll just special case the check in mtChecks and get rid of the ":" in the suffix. 
                        // That I think will benefit more customers, than giving a format error which they have to go and fix.
                        //
                        new ProxyAddressComposedRule(
                            new StringMaxLengthRule(256),
                            new FixProxyTargetAddressRule(),
                            new NoDuplicatesRule()
                        ),

                        // If UPN is valid, and samAccountName is Invalid, sync still works, so we check for invalid
                        // SamAccountName only if UPN isn't valid
                        // Max Length = 20
                        // Invalid Characters [ \ " | , \ : <  > + = ? * ]
                        new ComposedRule(StringLiterals.SamAccountName,
                            new SamAccountNameMaxLengthRule(),
                            new RegexRule(new Regex(@"[\\""|,/\[\]:<>+=;?*]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
                            new SamAccountNameDuplicateRule()
                        ),

                        // Checks for TargetAddress
                        new TargetAddressComposedRule(
                            new StringMaxLengthRule(255),
                            new FixProxyTargetAddressRule(),
                            new NoDuplicatesRule()
                        )
                    };

                    // Updated UPN 
                    //  Should confirm to rfc2822 -- checked in mtChecks
                    //  @ needs to be present -- checked in mtChecks as part of rfc2822
                    //  Length before @ = 48 -- checked in mtChecks Length
                    //  Length after  @ = 64 -- checked in mtChecks Length
                    //  Cannot contain space \ % & * + / = ?  { } | < > ( ) ; : , [ ] “ umlaut -- RegEx
                    //  @ cannot be first character -- RegEx
                    //  period (.), ampersand (&), space, or at sign (@) cannot be the last character -- RegEx 
                    //  No duplicates -- checked in mtChecks
                    if (!SettingsManager.Instance.UseAlternateLogin)
                    {
                        rules.Add(new ComposedRule(StringLiterals.UserPrincipalName,
                            new StringMaxLengthRule(113),
                            new RegexRule(Constants.InvalidUpnRegExRegex),
                            new RFC2822Rule(),
                            new NoDuplicatesRule()
                           ));
                    }

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
            StringLiterals.GroupType,
            StringLiterals.HomeMdb,
            StringLiterals.IsCriticalSystemObject,
            StringLiterals.Mail,
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
                string objectType = ComposedRule.GetObjectType(entry);

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
                            case "0x2000":
                            case "0x4000":
                            case "0x400000":
                            case "0x800000":
                            case "0x1000000":
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

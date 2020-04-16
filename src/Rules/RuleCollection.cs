using IdFix.Settings;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdFix.Rules
{
    abstract class RuleCollection
    {
        protected RuleCollection(LdapConnection connection, string distinguishedName)
        {
            this.Connection = connection;
            this.DistinguishedName = distinguishedName;
        }

        protected LdapConnection Connection { get; private set; }
        protected string DistinguishedName { get; private set; }

        public abstract string[] AttributesToQuery { get; }

        public string[] Rules { get; }

        public virtual Run()
        {
            long entryCount = 0;
            long errorCount = 0;
            long duplicateCount = 0;
            long displayCount = 0;

            PageResultRequestControl pageRequest = new PageResultRequestControl(1000);
            SearchRequest searchRequest = new SearchRequest(
                this.DistinguishedName,
                SettingsManager.Instance.Filter,
                System.DirectoryServices.Protocols.SearchScope.Subtree,
                this.AttributesToQuery);
            searchRequest.Controls.Add(pageRequest);
            SearchResponse searchResponse;
            this.OnStatusUpdate?.Invoke("Please wait while the LDAP Connection is established.");

            while (true)
            {
                searchResponse = (SearchResponse)this.Connection.SendRequest(searchRequest);

                // verify support for paged results
                if (searchResponse.Controls.Length != 1 || !(searchResponse.Controls[0] is PageResultResponseControl))
                {
                    this.OnStatusUpdate?.Invoke("The server cannot page the result set.");
                    throw new InvalidOperationException("The server cannot page the result set.");
                }

                PageResultResponseControl pageResponse = (PageResultResponseControl)searchResponse.Controls[0];

                foreach (SearchResultEntry entry in searchResponse.Entries)
                {


                    // TODO:: check for cancel - need to figure out how this works with background worker
                    //if (backgroundWorker1.CancellationPending)
                    //{
                    //    e.Cancel = true;
                    //    e.Result = StringLiterals.CancelQuery;
                    //    files.DeleteByType(FileTypes.Error);
                    //    files.DeleteByType(FileTypes.Duplicate);
                    //    return;
                    //}





                    entryCount++;

                    #region perform checks
                    try
                    {
                        string objectType = entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString();
                        if (SettingsManager.Instance.CurrentRuleMode == RuleMode.MultiTenant)
                        {
                            #region do MT checks
                            if (mtFilter(entry))
                            {
                                continue;
                            }

                            //Additional check for DisplayName. 
                            //  Value is non-blank if present
                            //  Max Length = 255. 
                            //The attribute being present check is done in mtChecks, however, the errorBlank check is done only if the attribute
                            //is missing, so that has been updated to check for blanks if attribute present.
                            mtChecks(entry, StringLiterals.DisplayName, 0, 255, null, null, false, false, true);

                            //Additional check for GivenName. 
                            //  Max Length = 63.
                            mtChecks(entry, StringLiterals.GivenName, 0, 63, null, null, false, false, false);

                            //New documentation doesn't say anything about mail not being whitespace nor rfc822 format, so pulling that out.
                            //It should just be unique.
                            //  Max Length = 256 -- See XL sheet
                            mtChecks(entry, StringLiterals.Mail, 0, 256, null, null, true, false, false);
                            //Updated check for MailNickName
                            //  Cannot start with period (.)
                            //  Max Length = 64, document doesn't restrict, schema says 64
                            mtChecks(entry, StringLiterals.MailNickName, 0, 64, new Regex(@"^[.]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), null, true, false, false);

                            //ProxyAddresses have additional requirements. 
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
                            if (entry.Attributes.Contains(StringLiterals.ProxyAddresses))
                            {
                                Regex invalidProxyAddressSMTPRegEx =
                                    new Regex(@"[\s<>()\;\,\[\]""]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                                Regex invalidProxyAddressRegEx =
                                    new Regex(@"[\s<>()\,\[\]""]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                                for (int i = 0; i <= entry.Attributes[StringLiterals.ProxyAddresses].Count - 1; i++)
                                {
                                    bool isSmtp = smtp.IsMatch(entry.Attributes[StringLiterals.ProxyAddresses][i].ToString());
                                    mtChecks(entry, StringLiterals.ProxyAddresses, i, 256,
                                        isSmtp ? invalidProxyAddressSMTPRegEx : invalidProxyAddressRegEx,
                                        isSmtp ? rfc2822 : null,
                                        true, false, false);
                                }
                            }

                            //If UPN is valid, and samAccountName is Invalid, sync still works, so we check for invalid
                            //SamAccountName only if UPN isn't valid
                            //Max Length = 20
                            //Invalid Characters [ \ " | , \ : <  > + = ? * ]
                            if (entry.Attributes.Contains(StringLiterals.SamAccountName) && !IsValidUpn(entry))
                            {
                                mtChecks(entry, StringLiterals.SamAccountName, 0,
                                    objectType.Equals("user", StringComparison.CurrentCultureIgnoreCase) ? 20 : 256,
                                    new Regex(@"[\\""|,/\[\]:<>+=;?*]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), null,
                                    entry.Attributes.Contains(StringLiterals.UserPrincipalName) ? false : true, false, false);
                            }

                            //Checks for TargetAddress
                            if (entry.Attributes.Contains(StringLiterals.TargetAddress))
                            {
                                //  Max Length = 255
                                //TargetAddress cannot contain space \ < > ( ) ; , [ ] " 
                                // There should be no ":" in the suffix part of TargetAddress
                                //TargetAddress must be unique
                                //SMTP should follow rfc2822
                                Regex invalidTargetAddressSMTPRegEx =
                                    new Regex(@"[\s\\<>()\;\,\[\]""]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                                Regex invalidTargetAddressRegEx =
                                    new Regex(@"[\s\\<>()\,\[\]""]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                                bool isSmtp = smtp.IsMatch(entry.Attributes[StringLiterals.TargetAddress][0].ToString());
                                mtChecks(entry, StringLiterals.TargetAddress, 0, 255,
                                    isSmtp ? invalidTargetAddressSMTPRegEx : invalidTargetAddressRegEx,
                                    isSmtp ? rfc2822 : null,
                                    true, false, false);
                            }

                            //Updated UPN 
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
                                mtChecks(entry, StringLiterals.UserPrincipalName, 0, 113, invalidUpnRegEx, rfc2822, true, false, false);
                            }

                            #endregion
                        }
                        else
                        {
                            #region do D checks
                            if (dFilter(entry))
                            {
                                continue;
                            }

                            dChecks(entry, StringLiterals.DisplayName, 0, 256, new Regex(@"^[\s]+|[\s]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), null, false,
                                objectType.Equals("group", StringComparison.CurrentCultureIgnoreCase) ? true : false);
                            dChecks(entry, StringLiterals.Mail, 0, 256, new Regex(@"[\s]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), rfc2822, true, false);
                            dChecks(entry, StringLiterals.MailNickName, 0, 64, new Regex(@"[\s\\!#$%&*+/=?^`{}|~<>()'\;\:\,\[\]""@]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), periods, true, true);
                            if (entry.Attributes.Contains(StringLiterals.ProxyAddresses))
                            {
                                for (int i = 0; i <= entry.Attributes[StringLiterals.ProxyAddresses].Count - 1; i++)
                                {
                                    dChecks(entry, StringLiterals.ProxyAddresses, i, 256, null,
                                        smtp.IsMatch(entry.Attributes[StringLiterals.ProxyAddresses][i].ToString()) ? rfc2822 : null, true, false);
                                }
                            }
                            // ### add check for sip proxies

                            if (entry.Attributes.Contains(StringLiterals.TargetAddress))
                            {
                                mtChecks(entry, StringLiterals.TargetAddress, 0, 256, null,
                                    smtp.IsMatch(entry.Attributes[StringLiterals.TargetAddress][0].ToString()) ? rfc2822 : null, false, false, false);
                            }
                            else
                            {
                                dChecks(entry, StringLiterals.TargetAddress, 0, 256, null, null, false, true);
                            }
                            #endregion
                        }
                    }
                    catch (Exception ex)
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            statusDisplay(StringLiterals.Exception + StringLiterals.DistinguishedName + ": "
                                + entry.Attributes[StringLiterals.DistinguishedName][0].ToString() + "  "
                                + ex.Message);
                        });
                    }
                    #endregion
                }

                this.OnStatusUpdate?.Invoke("Query Count: " + entryCount.ToString(CultureInfo.CurrentCulture)
            + "  Error Count: " + errorCount.ToString(CultureInfo.CurrentCulture)
            + "  Duplicate Check Count: " + duplicateCount.ToString(CultureInfo.CurrentCulture));


                // if this is true, there are no more pages to request
                if (pageResponse.Cookie.Length == 0)
                    break;

                pageRequest.Cookie = pageResponse.Cookie;

            }





        }

        public event OnStatusUpdateDelegate OnStatusUpdate;
    }
}

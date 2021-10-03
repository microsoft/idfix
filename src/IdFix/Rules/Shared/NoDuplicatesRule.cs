using IdFix.Settings;
using System;
using System.DirectoryServices.Protocols;

namespace IdFix.Rules.Shared
{
    /// <summary>
    /// Checks to see if this value is a duplicate
    /// </summary>
    /// <remarks>This rule MUST always appear last in a set of composed rules</remarks>
    class NoDuplicatesRule : Rule
    {
        /// <summary>
        /// Creates a new instance of the <see cref="NoDuplicatesRule"/> class
        /// </summary>
        /// <param name="fixer"></param>
        public NoDuplicatesRule(Func<SearchResultEntry, string, string> fixer = null)
            : base(fixer)
        {
        }

        /// <summary>
        /// Executes implementation for this rule
        /// </summary>
        /// <param name="parent">The composed rule containing this rule</param>
        /// <param name="entry">The search result we are checking</param>
        /// <param name="attributeValue">The current attribute value as pass through the chain</param>
        /// <returns>Either a success or error result</returns>
        public override RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue)
        {
            // record in memory the entry details with a list of the attributes that end up needing to be checked
            if (DuplicateStore.IsDuplicate(parent.AttributeName, attributeValue))
            {
                // we now need to execute some complicated logic from the original application depending
                // settings, etc. this isn't pretty but it follows the original for now

                var actionType = ActionType.Edit;
                var originalValue = entry.Attributes[parent.AttributeName][0].ToString();

                // if nothing else has changed the value prior to this (which is why no duplicate rule needs to be run last)
                if (originalValue == attributeValue)
                {
                    switch (parent.AttributeName.ToLowerInvariant())
                    {
                        case "proxyaddresses":
                            if (Constants.SMTPRegex.IsMatch(attributeValue))
                            {
                                if (entry.Attributes.Contains(StringLiterals.MailNickName))
                                {
                                    if (attributeValue.Substring(0, 5) == "SMTP:")
                                    {
                                        actionType = ActionType.Complete;
                                    }
                                }
                                else
                                {
                                    actionType = ActionType.Remove;
                                }
                            }
                            break;
                        case "userprincipalname":
                            if (entry.Attributes.Contains(StringLiterals.MailNickName))
                            {
                                actionType = ActionType.Complete;
                            }
                            break;
                        case "mail":
                            if (entry.Attributes.Contains(StringLiterals.MailNickName))
                            {
                                actionType = ActionType.Complete;
                            }
                            break;
                        case "mailnickname":
                            if (entry.Attributes.Contains(StringLiterals.HomeMdb))
                            {
                                actionType = ActionType.Complete;
                            }
                            break;
                        case "samaccountname":
                            if (entry.Attributes.Contains(StringLiterals.MailNickName))
                            {
                                actionType = ActionType.Complete;
                            }
                            break;
                    }
                }

                return this.GetErrorResult(ErrorType.Duplicate, attributeValue, actionType);
            }

            return this.GetSuccessResult();
        }
    }
}

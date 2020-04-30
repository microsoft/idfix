using IdFix.Rules.Shared;
using System;
using System.DirectoryServices.Protocols;

namespace IdFix.Rules.Dedicated
{
    /// <summary>
    /// Enforces that mail nickname can't be blank with a custom fixer method
    /// </summary>
    class MailNicknameBlankRule : BlankStringRule
    {
        /// <summary>
        /// Creates a new instance of the <see cref="MailNicknameBlankRule"/>
        /// </summary>
        public MailNicknameBlankRule(): base((entry, attributeValue) => {

            if (entry.Attributes.Contains(StringLiterals.GivenName))
            {
                attributeValue = entry.Attributes[StringLiterals.GivenName][0].ToString();
            }

            if (entry.Attributes.Contains(StringLiterals.Sn))
            {
                if (!string.IsNullOrEmpty(attributeValue))
                {
                    attributeValue += ".";
                }

                attributeValue += entry.Attributes[StringLiterals.Sn][0].ToString();
            }

            if (string.IsNullOrEmpty(attributeValue))
            {
                if (entry.Attributes.Contains(StringLiterals.SamAccountName))
                {
                    attributeValue = entry.Attributes[StringLiterals.SamAccountName][0].ToString();
                }
                else
                {
                    attributeValue = entry.Attributes[StringLiterals.Cn][0].ToString();
                }
            }

            return attributeValue;

        }) { }

        /// <summary>
        /// Executes implementation for this rule
        /// </summary>
        /// <param name="parent">The composed rule containing this rule</param>
        /// <param name="entry">The search result we are checking</param>
        /// <param name="attributeValue">The current attribute value as pass through the chain</param>
        /// <returns>Either a success or error result</returns>
        public override RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue)
        {
            string objectType = this.GetObjectType(entry);

            if (objectType.Equals("contact", StringComparison.CurrentCultureIgnoreCase) && parent.AttributeName == StringLiterals.MailNickName)
            {
                return base.Execute(parent, entry, attributeValue);
            }

            return this.GetSuccessResult();
        }
    }
}

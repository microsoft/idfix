using IdFix.Rules.Shared;
using System;
using System.DirectoryServices.Protocols;

namespace IdFix.Rules.Dedicated
{
    class MailNicknameBlankRule : BlankStringRule
    {
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

        public override RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue)
        {
            string objectType = entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString();

            if (objectType.Equals("contact", StringComparison.CurrentCultureIgnoreCase) && parent.AttributeName == StringLiterals.MailNickName)
            {
                return base.Execute(parent, entry, attributeValue);
            }

            return this.GetSuccessResult();
        }
    }
}

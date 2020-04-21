using IdFix.Rules.Shared;
using System;
using System.DirectoryServices.Protocols;

namespace IdFix.Rules.Dedicated
{
    class DisplayNameBlankRule : BlankStringRule
    {
        public DisplayNameBlankRule(): base((entry, attributeValue) => {
            var str = entry.Attributes[StringLiterals.Cn][0].ToString();
            return str.Length > 255 ? str.Substring(0, 255) : str;
        }) { }

        public override RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue)
        {
            string objectType = entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString();

            if (objectType.Equals("group", StringComparison.CurrentCultureIgnoreCase))
            {
                return base.Execute(parent, entry, attributeValue);
            }

            return this.GetSuccessResult();
        }
    }
}

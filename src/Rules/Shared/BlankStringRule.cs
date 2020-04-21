using IdFix.Settings;
using System;
using System.DirectoryServices.Protocols;
using System.Text.RegularExpressions;

namespace IdFix.Rules.Shared
{
    class BlankStringRule : Rule
    {
        public BlankStringRule(Func<SearchResultEntry, string, string> fixer) 
            : base(fixer)
        {
        }

        public override RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue)
        {
            if (string.IsNullOrEmpty(attributeValue))
            {
                var updatedValue = this.Fixer(entry, attributeValue);
                return this.GetErrorResult(ErrorType.Blank, updatedValue);
            }

            return this.GetSuccessResult();
        }
    }
}

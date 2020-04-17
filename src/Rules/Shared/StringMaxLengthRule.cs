using System;
using System.DirectoryServices.Protocols;
using System.Text.RegularExpressions;

namespace IdFix.Rules.Shared
{
    class StringMaxLengthRule : Rule
    {
        public StringMaxLengthRule(int maxLength, Func<SearchResultEntry, string, string> fixer = null)
            : base(fixer)
        {
            this.MaxLength = maxLength;
        }

        public int MaxLength { get; protected set; }

        public override RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue)
        {
            if (attributeValue.Length > this.MaxLength)
            {
                var updatedValue = this.Fixer != null ? this.Fixer(entry, attributeValue) : attributeValue.Substring(0, this.MaxLength);
                return this.GetErrorResult(StringLiterals.Length, updatedValue);
            }

            return this.GetSuccessResult();
        }
    }
}

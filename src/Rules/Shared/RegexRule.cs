using IdFix.Settings;
using System;
using System.DirectoryServices.Protocols;
using System.Text.RegularExpressions;

namespace IdFix.Rules.Shared
{
    class RegexRule : Rule
    {
        public RegexRule(Regex reg, Func<SearchResultEntry, string, string> fixer = null)
            : base(fixer)
        {
            this.CheckExpression = reg;
        }

        public Regex CheckExpression { get; private set; }

        public override RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue)
        {
            if (this.CheckExpression.IsMatch(attributeValue))
            {
                string updated;
                if (this.Fixer == null)
                {
                    var tempVal = this.CheckExpression.Replace(attributeValue, string.Empty);
                    updated = string.IsNullOrEmpty(tempVal) ? entry.Attributes[StringLiterals.Cn][0].ToString() : tempVal;
                }
                else
                {
                    updated = this.Fixer(entry, attributeValue);
                }

                return this.GetErrorResult(ErrorType.Character, updated);
            }

            return this.GetSuccessResult();
        }
    }
}

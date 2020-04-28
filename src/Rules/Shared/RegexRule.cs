using IdFix.Settings;
using System;
using System.DirectoryServices.Protocols;
using System.Text.RegularExpressions;

namespace IdFix.Rules.Shared
{
    /// <summary>
    /// Rule used to find character errors within strings based on a regular expression pattern
    /// </summary>
    class RegexRule : Rule
    {
        /// <summary>
        /// Creates a new instance of the <see cref="RegexRule"/> class
        /// </summary>
        /// <param name="reg">The regular expression to use</param>
        /// <param name="fixer">An optional function used to "fix" the error and suggest a replacement</param>
        public RegexRule(Regex reg, Func<SearchResultEntry, string, string> fixer = null)
            : base(fixer)
        {
            this.CheckExpression = reg;
        }

        /// <summary>
        /// Gets the expression used by this rule to check for bad characters
        /// </summary>
        public Regex CheckExpression { get; private set; }

        /// <summary>
        /// Executes implementation for this rule
        /// </summary>
        /// <param name="parent">The composed rule containing this rule</param>
        /// <param name="entry">The search result we are checking</param>
        /// <param name="attributeValue">The current attribute value as pass through the chain</param>
        /// <returns>Either a success or error result</returns>
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

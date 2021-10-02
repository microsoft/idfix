using IdFix.Settings;
using System;
using System.DirectoryServices.Protocols;

namespace IdFix.Rules.Shared
{
    /// <summary>
    /// Rule to check that a given attribute has a value (exists and is not null or empty)
    /// </summary>
    class BlankStringRule : Rule
    {
        /// <summary>
        /// Creates a new instnace of the <see cref="BlankStringRule"/> class.
        /// </summary>
        /// <param name="fixer">Afunction to "fix" the empty value with a suggested edit</param>
        public BlankStringRule(Func<SearchResultEntry, string, string> fixer) 
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
            if (string.IsNullOrEmpty(attributeValue))
            {
                var updatedValue = this.Fixer(entry, attributeValue);
                return this.GetErrorResult(ErrorType.Blank, updatedValue);
            }

            return this.GetSuccessResult();
        }
    }
}

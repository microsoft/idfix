using IdFix.Settings;
using System;
using System.DirectoryServices.Protocols;

namespace IdFix.Rules.Shared
{
    /// <summary>
    /// Rule used to check the maximum allowed length for a given field
    /// </summary>
    class StringMaxLengthRule : Rule
    {
        /// <summary>
        /// Creates a new instance of the <see cref="StringMaxLengthRule"/> class
        /// </summary>
        /// <param name="maxLength">The max length allowed for the value (value.length > maxLength)</param>
        /// <param name="fixer">A function used to suggest a replacement value for this missing string</param>
        public StringMaxLengthRule(int maxLength, Func<SearchResultEntry, string, string> fixer = null)
            : base(fixer)
        {
            this.MaxLength = maxLength;
        }

        /// <summary>
        /// Gets the length this rule uses to check max length
        /// </summary>
        public int MaxLength { get; protected set; }

        /// <summary>
        /// Executes implementation for this rule
        /// </summary>
        /// <param name="parent">The composed rule containing this rule</param>
        /// <param name="entry">The search result we are checking</param>
        /// <param name="attributeValue">The current attribute value as pass through the chain</param>
        /// <returns>Either a success or error result</returns>
        public override RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue)
        {
            if (attributeValue.Length > this.MaxLength)
            {
                var updatedValue = this.Fixer != null ? this.Fixer(entry, attributeValue) : attributeValue.Substring(0, this.MaxLength);
                return this.GetErrorResult(ErrorType.Length, updatedValue);
            }

            return this.GetSuccessResult();
        }
    }
}

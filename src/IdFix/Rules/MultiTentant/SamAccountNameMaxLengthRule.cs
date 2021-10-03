using IdFix.Rules.Shared;
using System;
using System.DirectoryServices.Protocols;

namespace IdFix.Rules.MultiTentant
{
    /// <summary>
    /// Handles special logic for the sam account name max length based on object type
    /// </summary>
    class SamAccountNameMaxLengthRule : StringMaxLengthRule
    {
        /// <summary>
        /// Creates a new instance of the <see cref="SamAccountNameMaxLengthRule"/> class
        /// </summary>
        public SamAccountNameMaxLengthRule() : base(256) { }

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

            this.MaxLength = objectType.Equals("user", StringComparison.CurrentCultureIgnoreCase) ? 20 : 256;

            return base.Execute(parent, entry, attributeValue);
        }
    }
}

using System.DirectoryServices.Protocols;
using IdFix.Rules.Shared;

namespace IdFix.Rules.MultiTentant
{
    /// <summary>
    /// Handles special logic for the SAM account name not allowing duplicates
    /// </summary>
    class SamAccountNameDuplicateRule : NoDuplicatesRule
    {
        /// <summary>
        /// Executes implementation for this rule
        /// </summary>
        /// <param name="parent">The composed rule containing this rule</param>
        /// <param name="entry">The search result we are checking</param>
        /// <param name="attributeValue">The current attribute value as pass through the chain</param>
        /// <returns>Either a success or error result</returns>
        public override RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue)
        {
            if (entry.Attributes.Contains(StringLiterals.UserPrincipalName))
            {
                return this.GetSuccessResult();
            }

            return base.Execute(parent, entry, attributeValue);
        }
    }
}

using IdFix.Rules.Shared;
using System;
using System.DirectoryServices.Protocols;

namespace IdFix.Rules.Dedicated
{
    /// <summary>
    /// Rule to ensure that displayName of a group is not blank
    /// </summary>
    class DisplayNameBlankRule : BlankStringRule
    {
        /// <summary>
        /// Creates a new instance of <see cref="DisplayNameBlankRule"/>
        /// </summary>
        public DisplayNameBlankRule(): base((entry, attributeValue) => {
            var str = entry.Attributes[StringLiterals.Cn][0].ToString();
            return str.Length > 255 ? str.Substring(0, 255) : str;
        }) { }

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

            if (objectType.Equals("group", StringComparison.CurrentCultureIgnoreCase))
            {
                return base.Execute(parent, entry, attributeValue);
            }

            return this.GetSuccessResult();
        }
    }
}

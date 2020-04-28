using IdFix.Settings;
using System.DirectoryServices.Protocols;

namespace IdFix.Rules.Shared
{
    /// <summary>
    /// A special rule with the business logic to fix proxy and target addresses mimicing the original code
    /// </summary>
    class FixProxyTargetAddressRule : Rule
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
            int colonPosn = attributeValue.IndexOf(":");
            if (colonPosn > 0)
            {
                //In case the suffix has a colon, we need to show a character error.
                //And replace it.
                string suffix = attributeValue.Substring(colonPosn + 1);
                if (suffix.Contains(":"))
                {
                    return this.GetErrorResult(ErrorType.Character, attributeValue.Substring(0, colonPosn) + ":" + suffix.Replace(":", ""));
                }
            }

            return this.GetSuccessResult();
        }
    }
}

using IdFix.Settings;
using System.DirectoryServices.Protocols;
using System.Text.RegularExpressions;

namespace IdFix.Rules.Dedicated
{
    /// <summary>
    /// A rule to process mail nicknames for periods and suggest corrections
    /// </summary>
    class MailNickNamePeriodsRule : Rule
    {
        public MailNickNamePeriodsRule() : base((entry, attributeValue) =>
        {
            attributeValue = new Regex(@"^[.]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Replace(attributeValue, "");
            if (string.IsNullOrEmpty(attributeValue))
            {
                attributeValue = entry.Attributes[StringLiterals.Cn][0].ToString();
            }

            attributeValue = new Regex(@"\.+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Replace(attributeValue, "");
            if (string.IsNullOrEmpty(attributeValue))
            {
                attributeValue = entry.Attributes[StringLiterals.Cn][0].ToString();
            }

            return attributeValue;
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
            if (Constants.PeriodsRegex.IsMatch(attributeValue))
            {
                this.GetErrorResult(ErrorType.Format, this.Fixer(entry, attributeValue));
            }

            return this.GetSuccessResult();
        }
    }
}

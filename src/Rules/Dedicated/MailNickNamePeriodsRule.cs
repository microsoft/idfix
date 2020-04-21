using IdFix.Settings;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IdFix.Rules.Dedicated
{
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

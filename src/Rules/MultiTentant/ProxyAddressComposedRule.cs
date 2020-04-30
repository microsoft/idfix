using IdFix.Rules.Shared;
using IdFix.Settings;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Text.RegularExpressions;

namespace IdFix.Rules.MultiTentant
{
    /// <summary>
    /// Handles the special case logic for ProxyAddresses
    /// </summary>
    class ProxyAddressComposedRule : ComposedRule
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ProxyAddressComposedRule"/> class
        /// </summary>
        /// <param name="additionalRules">Any additional rules to include within this special compound rule</param>
        public ProxyAddressComposedRule(params Rule[] additionalRules)
            : base(StringLiterals.ProxyAddresses, additionalRules) { }

        /// <summary>
        /// Executes implementation for this rule
        /// </summary>
        /// <param name="entry">The search result we are checking</param>
        /// <returns>Either a success or error result</returns>
        public override ComposedRuleResult[] Execute(SearchResultEntry entry)
        {
            var result = this.InitResult(entry, out bool isValuePresent);

            if (!isValuePresent)
            {
                result.Success = true;
                return new ComposedRuleResult[] { result };
            }

            var composedResultCollector = new List<ComposedRuleResult>();

            for (int i = 0; i <= entry.Attributes[this.AttributeName].Count - 1; i++)
            {
                result = this.InitResult(entry, out isValuePresent);
                var resultsCollector = new List<RuleResult>();

                var attributeValue = entry.Attributes[this.AttributeName][i].ToString();
                result.OriginalValue = attributeValue;

                // reset rule list
                var rulesList = this.Rules.ToList();
                // then need to do special cases to add in the rules based on what is needed
                if (Constants.SMTPRegex.IsMatch(attributeValue))
                {
                    rulesList.Add(new RegexRule(Constants.InvalidProxyAddressSMTPRegex));
                    rulesList.Add(new RFC2822Rule());
                }
                else
                {
                    var regexStr = Constants.InvalidProxyAddressRegex;

                    // we need to special handle the x400 & x500 rules
                    if (new Regex(@"^x\.?400", RegexOptions.IgnoreCase).IsMatch(attributeValue))
                    {
                        regexStr = Constants.InvalidX400ProxyAddressRegex;
                    }
                    else if (new Regex(@"^x\.?500", RegexOptions.IgnoreCase).IsMatch(attributeValue)) {
                        regexStr = Constants.InvalidX500ProxyAddressRegex;
                    }

                    rulesList.Add(new RegexRule(regexStr));
                }

                foreach (var rule in rulesList)
                {
                    var r = rule.Execute(this, entry, attributeValue);
                    if (!r.Success)
                    {
                        // we need to mimic the previous logic that updated value as
                        // the entry was processed
                        attributeValue = r.UpdatedValue;

                        result.ProposedAction = ActionType.Edit;
                    }
                    resultsCollector.Add(r);
                }

                composedResultCollector.Add(this.FinalizeResult(result, resultsCollector));
            }

            return composedResultCollector.ToArray();
        }
    }
}

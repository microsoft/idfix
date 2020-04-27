using IdFix.Rules.Shared;
using IdFix.Settings;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdFix.Rules.MultiTentant
{
    /// <summary>
    /// Handles the special case logic for ProxyAddresses
    /// </summary>
    class TargetAddressComposedRule : ComposedRule
    {
        public TargetAddressComposedRule(params Rule[] additionalRules)
            : base(StringLiterals.TargetAddress, additionalRules) { }

        public override ComposedRuleResult[] Execute(SearchResultEntry entry)
        {
            var result = this.InitResult(entry, out bool isValuePresent);

            if (!isValuePresent)
            {
                // base already handles the case of attribute not present and check for existing
                // blank rule so we return that result
                return base.Execute(entry);
            }

            var composedResultCollector = new List<ComposedRuleResult>();

            var attributeValue = result.OriginalValue;

            // reset rule list
            var rulesList = this.Rules.ToList();
            // then need to do special cases to add in the rules based on what is needed
            if (Constants.SMTPRegex.IsMatch(attributeValue))
            {
                rulesList.Add(new RegexRule(Constants.InvalidTargetAddressSMTPRegEx));
                rulesList.Add(new RFC2822Rule());
            }
            else
            {
                rulesList.Add(new RegexRule(Constants.InvalidTargetAddressRegEx));
            }

            var resultsCollector = new List<RuleResult>();

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

            return composedResultCollector.ToArray();
        }
    }
}

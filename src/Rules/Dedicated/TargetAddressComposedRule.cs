using IdFix.Rules.MultiTentant;
using IdFix.Rules.Shared;
using IdFix.Settings;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdFix.Rules.Dedicated
{
    /// <summary>
    /// Handles the special case logic for ProxyAddresses
    /// </summary>
    class TargetAddressComposedRule : ComposedRule
    {
        public TargetAddressComposedRule(params Rule[] additionalRules)
            : base(StringLiterals.TargetAddress, additionalRules) { }

        public override ComposedRuleResult Execute(SearchResultEntry entry)
        {
            if (!entry.Attributes.Contains(StringLiterals.TargetAddress))
            {
                // base already handles the case of attribute not present and check for existing
                // blank rule so we return that result
                return base.Execute(entry);
            }

            var result = new ComposedRuleResult();
            var resultsCollector = new List<RuleResult>();

            var attributeValue = entry.Attributes[StringLiterals.ProxyAddresses][0].ToString();
            bool isSmtp = Constants.SMTPRegex.IsMatch(attributeValue);

            var rulesList = this.Rules.ToList();

            // then need to do special cases to add in the rules based on what is needed
            if (isSmtp)
            {
                rulesList.Add(new RFC2822Rule());
            }

            foreach (var rule in rulesList)
            {
                var r = rule.Execute(this, entry, attributeValue);
                if (!r.Success)
                {
                    // we need to mimic the previous logic that updated value as
                    // the entry was processed
                    attributeValue = r.UpdatedValue;
                }
                resultsCollector.Add(r);
            }

            // account for success if an entry doesn't have a given field
            result.Success = resultsCollector.Count < 1 || resultsCollector.All(r => r.Success);
            result.Results = resultsCollector.ToArray();
            return result;
        }
    }
}

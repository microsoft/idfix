using IdFix.Rules.Shared;
using IdFix.Settings;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;

namespace IdFix.Rules
{
    public class ComposedRuleResult
    {
        public ComposedRuleResult()
        {
            this.Success = true;
        }

        /// <summary>
        /// The distinguised name of the entity to which this composed rule result applies
        /// </summary>
        public string EntityDistinguishedName { get; set; }

        /// <summary>
        /// The type of this entity
        /// </summary>
        public string ObjectType { get; set; }

        /// <summary>
        /// Tracks the attribute name to which this result applies
        /// </summary>
        public string AttributeName { get; set; }

        /// <summary>
        /// The original value found for the attribute covered by this composed rule
        /// </summary>
        public string OriginalValue { get; set; }

        /// <summary>
        /// Updated value from any fixes run by the rules
        /// </summary>
        public ActionType ProposedAction { get; set; }

        /// <summary>
        /// Updated value from any fixes run by the rules
        /// </summary>
        public string ProposedValue { get; set; }

        /// <summary>
        /// Indicates the overall success of running the rule (total pass/fail)
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The set of results provided by all of the rules within this composed rule
        /// </summary>
        public RuleResult[] Results { get; set; }

        /// <summary>
        /// For each result with an error, gathers the messages up into a comma seperated string
        /// </summary>
        /// <returns></returns>
        public string ErrorsToString()
        {
            if (this.Results == null)
            {
                return string.Empty;
            }
            return string.Join(", ", this.Results.Where(r => !r.Success).Select(r => r.ErrorTypeFlags));
        }
    }

    interface IComposedRule
    {
        string AttributeName { get; }
        ComposedRuleResult[] Execute(SearchResultEntry entry);
    }

    class ComposedRule : IComposedRule
    {
        public ComposedRule(string attributeName, params Rule[] rules)
        {
            this.AttributeName = attributeName;
            this.Rules = rules;
        }

        public string AttributeName { get; private set; }
        public Rule[] Rules { get; private set; }

        public virtual ComposedRuleResult[] Execute(SearchResultEntry entry)
        {
            var result = this.InitResult(entry, out bool isValuePresent);

            var resultsCollector = new List<RuleResult>();

            // need to handle the case where entry doesn't have attribute named but does
            // have a no blanks rule
            if (!isValuePresent)
            {
                var rs = this.Rules.OfType<BlankStringRule>();
                if (rs.Any())
                {
                    // we do not allow blanks so include just that error and done
                    resultsCollector.Add(rs.First().Execute(this, entry, null));
                }
            }
            else
            {
                // get the value of the attribute for this composed rule
                var attributeValue = result.OriginalValue;

                // now we process each of the rules contained within this compound rule
                foreach (var rule in this.Rules)
                {
                    var r = rule.Execute(this, entry, attributeValue);
                    if (!r.Success)
                    {
                        // we need to mimic the previous logic that updated value as
                        // the entry was processed
                        attributeValue = r.UpdatedValue;
                    }

                    // add our result to the collector
                    resultsCollector.Add(r);
                }
            }

            return new ComposedRuleResult[] { this.FinalizeResult(result, resultsCollector) };
        }

        protected ComposedRuleResult InitResult(SearchResultEntry entry, out bool isValuePresent)
        {
            isValuePresent = false;

            var result = new ComposedRuleResult()
            {
                AttributeName = this.AttributeName,
                EntityDistinguishedName = entry.Attributes[StringLiterals.DistinguishedName][0].ToString(),
                ObjectType = entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString(),
                OriginalValue = null,
                ProposedAction = ActionType.None
            };

            if (entry.Attributes.Contains(this.AttributeName))
            {
                isValuePresent = true;
                result.OriginalValue = entry.Attributes[this.AttributeName][0].ToString();
            }

            return result;
        }

        protected ComposedRuleResult FinalizeResult(ComposedRuleResult result, List<RuleResult> resultsCollector)
        {
            // set all the rule results on the compound result
            result.Results = resultsCollector.ToArray();

            // account for success if an entry doesn't have a given field
            result.Success = resultsCollector.Count < 1 || resultsCollector.All(r => r.Success);

            if (!result.Success)
            {
                // update the proposed action to edit for all errors by default
                result.ProposedAction = ActionType.Edit;

                // we propose the final result's updated value where success is false, matching the original logic
                result.ProposedValue = result.Results.Where(r => !r.Success).Last().UpdatedValue;
            }

            return result;
        }
    }
}

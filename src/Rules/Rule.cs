using IdFix.Rules.Shared;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdFix.Rules
{
    /// <summary>
    /// The result provided by a single rule
    /// </summary>
    class RuleResult
    {
        public RuleResult(bool success = true)
        {
            this.Success = success;
            this.Error = string.Empty;
            this.UpdatedValue = string.Empty;
        }

        public bool Success { get; set; }
        public string Error { get; set; }
        public string UpdatedValue { get; set; }
    }

    abstract class Rule
    {
        protected Rule(Func<SearchResultEntry, string, string> fixer = null)
        {
            this.Fixer = fixer;
        }

        public Func<SearchResultEntry, string, string> Fixer { get; private set; }

        public abstract RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue);

        protected RuleResult GetSuccessResult()
        {
            return new RuleResult();
        }

        protected RuleResult GetErrorResult(string errorMessage, string updatedValue)
        {
            return new RuleResult(false)
            {
                Error = errorMessage,
                UpdatedValue = updatedValue
            };
        }
    }

    class ComposedRuleResult
    {
        public ComposedRuleResult()
        {
            this.Success = true;
        }

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
            return string.Join(", ", this.Results.Where(r => !r.Success).Select(r => r.Error));
        }
    }

    interface IComposedRule
    {
        ComposedRuleResult Execute(SearchResultEntry entry);
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

        public virtual ComposedRuleResult Execute(SearchResultEntry entry)
        {
            var result = new ComposedRuleResult();
            var resultsCollector = new List<RuleResult>();

            // need to handle the case where entry doesn't have attribute named but does
            // have a no blanks rule
            if (!entry.Attributes.Contains(this.AttributeName))
            {
                var rules = this.Rules.OfType<BlankStringRule>();
                if (rules.Any())
                {
                    var rule = rules.First();
                    // we do not allow blanks so include just that error and done
                    resultsCollector.Add(rule.Execute(this, entry, null));
                }
            }
            else
            {
                // get the value of the attribute for this composed rule
                var attributeValue = entry.Attributes[this.AttributeName][0].ToString();

                foreach (var rule in this.Rules)
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
            }

            // account for success if an entry doesn't have a given field
            result.Success = resultsCollector.Count < 1 || resultsCollector.All(r => r.Success);
            result.Results = resultsCollector.ToArray();
            return result;
        }
    }
}

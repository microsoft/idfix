using IdFix.Rules.Shared;
using IdFix.Settings;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;

namespace IdFix.Rules
{
    #region ComposedRuleResult

    /// <summary>
    /// Contains the results of executing a composed rule
    /// </summary>
    public class ComposedRuleResult
    {
        public ComposedRuleResult()
        {
            this.Success = true;
        }

        /// <summary>
        /// The distinguished name of the entity to which this composed rule result applies
        /// </summary>
        public string EntityDistinguishedName { get; set; }

        /// <summary>
        /// The common name of the entity to which this composed rule result applies
        /// </summary>
        public string EntityCommonName { get; set; }

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

    #endregion

    #region IComposedRule

    /// <summary>
    /// Interface representing the methods and properties a composed rule must implement
    /// </summary>
    interface IComposedRule
    {
        /// <summary>
        /// Gets the name of the <see cref="SearchResultEntry"/> attribute this rule checks
        /// </summary>
        string AttributeName { get; }

        /// <summary>
        /// Executes this composed rule
        /// </summary>
        /// <param name="entry">The <see cref="SearchResultEntry"/> to check</param>
        /// <returns>A set of composed rule results</returns>
        ComposedRuleResult[] Execute(SearchResultEntry entry);
    }

    #endregion

    /// <summary>
    /// A composed rule contains one of more <see cref="Rule"/> instances and is bound to an attribute name.
    /// It runs all of the contained rules against the given attribute producing one or more composed rule results
    /// </summary>
    class ComposedRule : IComposedRule
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ComposedRule"/> class
        /// </summary>
        /// <param name="attributeName">The name of the attribute this compound rule is set to check</param>
        /// <param name="rules">Set of rules comprising the checks done against the value of the <paramref name="attributeName"/> attribute</param>
        public ComposedRule(string attributeName, params Rule[] rules)
        {
            this.AttributeName = attributeName;
            this.Rules = rules;
        }

        /// <summary>
        /// Gets the object type for a given <see cref="SearchResultEntry"/>
        /// </summary>
        /// <param name="entry">The <see cref="SearchResultEntry"/> whose object type we want</param>
        /// <returns>The object type represented as a string</returns>
        public static string GetObjectType(SearchResultEntry entry)
        {
            return entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString();
        }

        #region props

        /// <summary>
        /// Gets the name of the <see cref="SearchResultEntry"/> attribute this rule checks
        /// </summary>
        public string AttributeName { get; private set; }

        /// <summary>
        /// The set of rules contained within this compound rule
        /// </summary>
        public Rule[] Rules { get; private set; }

        #endregion

        #region Execute

        /// <summary>
        /// Executes this composed rule
        /// </summary>
        /// <param name="entry">The <see cref="SearchResultEntry"/> to check</param>
        /// <returns>A set of composed rule results</returns>
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

        #endregion

        #region InitResult

        /// <summary>
        /// Creates a consistent base <see cref="ComposedRuleResult"/> instance with default values
        /// </summary>
        /// <param name="entry">The entry for which we are creating a result</param>
        /// <param name="isValuePresent">Allows calling code to know if the <paramref name="entry"/> has a value for the given <see cref="AttributeName"/></param>
        /// <returns></returns>
        protected ComposedRuleResult InitResult(SearchResultEntry entry, out bool isValuePresent)
        {
            isValuePresent = false;

            var result = new ComposedRuleResult()
            {
                AttributeName = this.AttributeName,
                EntityDistinguishedName = entry.Attributes[StringLiterals.DistinguishedName][0].ToString(),
                EntityCommonName = entry.Attributes[StringLiterals.Cn][0].ToString(),
                ObjectType = ComposedRule.GetObjectType(entry),
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

        #endregion

        #region FinalizeResult

        /// <summary>
        /// Finalizes the <paramref name="result"/> based on the individual <see cref="RuleResult"/> values contained in <paramref name="resultsCollector"/>
        /// </summary>
        /// <param name="result">The result to update</param>
        /// <param name="resultsCollector">The set of <see cref="RuleResult"/> values used to determine the compound outcome</param>
        /// <returns></returns>
        protected ComposedRuleResult FinalizeResult(ComposedRuleResult result, List<RuleResult> resultsCollector)
        {
            // set all the rule results on the compound result
            result.Results = resultsCollector.ToArray();

            // account for success if an entry doesn't have a given field
            result.Success = resultsCollector.Count < 1 || resultsCollector.All(r => r.Success);

            if (!result.Success)
            {
                // we propose the last proposed action, matching the original logic
                result.ProposedAction = result.Results.Where(r => !r.Success).Last().ProposedAction;

                // we propose the final result's updated value where success is false, matching the original logic
                result.ProposedValue = result.Results.Where(r => !r.Success).Last().UpdatedValue;
            }

            return result;
        }

        #endregion
    }
}

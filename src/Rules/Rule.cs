using IdFix.Settings;
using System;
using System.DirectoryServices.Protocols;

namespace IdFix.Rules
{
    #region RuleResult

    /// <summary>
    /// The result provided by a single rule
    /// </summary>
    public class RuleResult
    {
        /// <summary>
        ///  Creates a new instance of the <see cref="RuleResult"/> class
        /// </summary>
        /// <param name="success">Marks this result as successful (default) or not</param>
        public RuleResult(bool success = true)
        {
            this.Success = success;
            this.ErrorTypeFlags = ErrorType.None;
            this.ProposedAction = ActionType.None;
            this.UpdatedValue = string.Empty;
        }

        /// <summary>
        /// Indicates if this rule was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Any error flags associated with a failed rule
        /// </summary>
        public ErrorType ErrorTypeFlags { get; set; }

        /// <summary>
        /// A proposed action for the user to take
        /// </summary>
        public ActionType ProposedAction { get; set; }

        /// <summary>
        /// A proposed updated value for failed rules
        /// </summary>
        public string UpdatedValue { get; set; }
    }

    #endregion

    /// <summary>
    /// Defines the basic methods of a single rule used to check an attributes value
    /// </summary>
    abstract class Rule
    {
        /// <summary>
        /// Used by implementing classes to establish basic object behavior
        /// </summary>
        /// <param name="fixer">A fixer used to determine a proposed value for a given failed attribute value</param>
        protected Rule(Func<SearchResultEntry, string, string> fixer = null)
        {
            this.Fixer = fixer;
        }

        /// <summary>
        /// Access to the fixer function
        /// </summary>
        public Func<SearchResultEntry, string, string> Fixer { get; private set; }

        /// <summary>
        /// When implemented by child classes executes this rule and returns a result
        /// </summary>
        /// <param name="parent">The composed rule containing this rule</param>
        /// <param name="entry">The search result we are checking</param>
        /// <param name="attributeValue">The current attribute value as pass through the chain</param>
        /// <returns>Either a success or error result</returns>
        public abstract RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue);

        /// <summary>
        /// Gets a default success result
        /// </summary>
        /// <returns>A new RuleResult indicating a passed rule</returns>
        protected RuleResult GetSuccessResult()
        {
            return new RuleResult();
        }

        /// <summary>
        /// Gets an error result based on the supplied paramters
        /// </summary>
        /// <param name="errType">The type of error</param>
        /// <param name="updatedValue">The proposed value</param>
        /// <param name="proposedAction">The proposed action</param>
        /// <returns>A new RuleResult indicating a failed rule</returns>
        protected RuleResult GetErrorResult(ErrorType errType, string updatedValue, ActionType proposedAction = ActionType.Edit)
        {
            return new RuleResult(false)
            {
                ErrorTypeFlags = errType,
                ProposedAction = proposedAction,
                UpdatedValue = updatedValue
            };
        }

        /// <summary>
        /// Gets the object type as a string for the given <paramref name="entry"/>
        /// </summary>
        /// <param name="entry"><see cref="SearchResultEntry"/> whose object type we want</param>
        /// <returns></returns>
        protected string GetObjectType(SearchResultEntry entry)
        {
            return ComposedRule.GetObjectType(entry);
        }
    }
}
    

using IdFix.Settings;
using System;
using System.DirectoryServices.Protocols;

namespace IdFix.Rules
{
    /// <summary>
    /// The result provided by a single rule
    /// </summary>
    public class RuleResult
    {
        public RuleResult(bool success = true)
        {
            this.Success = success;
            this.ErrorTypeFlags = ErrorType.None;
            this.ProposedAction = ActionType.None;
            this.UpdatedValue = string.Empty;
        }

        public bool Success { get; set; }
        public ErrorType ErrorTypeFlags { get; set; }
        public ActionType ProposedAction { get; set; }
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

        protected RuleResult GetErrorResult(ErrorType errType, string updatedValue, ActionType proposedAction = ActionType.Edit)
        {
            return new RuleResult(false)
            {
                ErrorTypeFlags = errType,
                ProposedAction = proposedAction,
                UpdatedValue = updatedValue
            };
        }
    }
}
    

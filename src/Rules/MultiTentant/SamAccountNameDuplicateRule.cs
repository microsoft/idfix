using System.DirectoryServices.Protocols;
using IdFix.Rules.Shared;

namespace IdFix.Rules.MultiTentant
{
    class SamAccountNameDuplicateRule : NoDuplicatesRule
    {
        public override RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue)
        {
            if (entry.Attributes.Contains(StringLiterals.UserPrincipalName))
            {
                return this.GetSuccessResult();
            }

            return base.Execute(parent, entry, attributeValue);
        }
    }
}

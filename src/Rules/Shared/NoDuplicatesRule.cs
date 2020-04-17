using System;
using System.Collections.Generic;
using System;
using System.DirectoryServices.Protocols;
using System.Text.RegularExpressions;

namespace IdFix.Rules.Shared
{
    class NoDuplicatesRule : Rule
    {
        public NoDuplicatesRule(Func<SearchResultEntry, string, string> fixer = null)
            : base(fixer)
        {
        }

        public override RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue)
        {
            // record in memory the entry details with a list of the attributes that end up needing to be checked
            if (DuplicateStore.IsDuplicate(parent.AttributeName, attributeValue))
            {
                return this.GetErrorResult(StringLiterals.Duplicate, attributeValue);
            }

            return this.GetSuccessResult();
        }
    }
}

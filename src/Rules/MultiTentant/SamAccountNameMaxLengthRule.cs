using IdFix.Rules.Shared;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdFix.Rules.MultiTentant
{
    class SamAccountNameMaxLengthRule : StringMaxLengthRule
    {
        public SamAccountNameMaxLengthRule() : base(256) { }

        public override RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue)
        {
            string objectType = entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString();

            this.MaxLength = objectType.Equals("user", StringComparison.CurrentCultureIgnoreCase) ? 20 : 256;

            return base.Execute(parent, entry, attributeValue);
        }
    }
}

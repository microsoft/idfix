using System;
using System.DirectoryServices.Protocols;

namespace IdFix.Rules.Shared
{
    class FixProxyTargetAddressRule : Rule
    {
        public override RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue)
        {
            int colonPosn = attributeValue.IndexOf(":");
            if (colonPosn > 0)
            {
                //In case the suffix has a colon, we need to show a character error.
                //And replace it.
                string suffix = attributeValue.Substring(colonPosn + 1);
                if (suffix.Contains(":"))
                {
                    return this.GetErrorResult(StringLiterals.Character, attributeValue.Substring(0, colonPosn) + ":" + suffix.Replace(":", ""));

                }
            }

            return this.GetSuccessResult();
        }
    }
}

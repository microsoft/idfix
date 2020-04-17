using IdFix.Rules.Shared;
using IdFix.Settings;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IdFix.Rules.MultiTentant
{
    class RFC2822Rule : Rule
    {
        public RFC2822Rule() : base() { }

        public override RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue)
        {
            var tldList = new ValidTLDList();
            bool success = false;
            var errors = new List<string>();
            string validateAttribute = Constants.SMTPRegex.IsMatch(attributeValue) ? attributeValue.Substring(attributeValue.IndexOf(":") + 1) : attributeValue;

            if (validateAttribute.LastIndexOf(".") != -1)
            {
                string tldDomain = validateAttribute.ToLowerInvariant().Substring(validateAttribute.LastIndexOf("."));
                if (tldDomain.Length > 1)
                {
                    tldDomain = tldDomain.Substring(tldDomain.IndexOf(".") + 1);
                    if (!tldList.Contains(tldDomain))
                    {
                        success = false;
                        errors.Add(StringLiterals.TopLevelDomain);
                    }
                }
            }

            if (!Constants.DomainPartRegex.IsMatch(validateAttribute))
            {
                success = false;
                errors.Add(StringLiterals.DomainPart);
                if (validateAttribute.LastIndexOf("@") != -1)
                {
                    validateAttribute = validateAttribute.Substring(0, validateAttribute.LastIndexOf("@") + 1)
                        + (new Regex(@"[^a-z0-9.-]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Replace(validateAttribute.Substring(validateAttribute.LastIndexOf("@") + 1), "");
                }
            }

            if (!Constants.LocalPartRegex.IsMatch(validateAttribute) || (validateAttribute.Split('@').Length - 1 > 1))
            {
                success = false;
                errors.Add(StringLiterals.LocalPart);
                if (validateAttribute.LastIndexOf("@") != -1)
                {
                    string validateLocal = (new Regex(@"[^a-z0-9.!#$%&'*+/=?^_`{|}~-]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Replace(validateAttribute.Substring(0, validateAttribute.LastIndexOf("@")), "");
                    validateAttribute = (validateLocal + validateAttribute.Substring(validateAttribute.LastIndexOf("@"))).Replace(".@", "@");
                }
            }

            if (!Constants.Rfc2822Regex.IsMatch(validateAttribute))
            {
                success = false;
                errors.Add(StringLiterals.Format);
            }

            if (success)
            {
                return this.GetSuccessResult();
            }
            else
            {
                attributeValue = Constants.SMTPRegex.IsMatch(attributeValue)
                    ? attributeValue.Substring(0, attributeValue.IndexOf(":") + 1).Trim() + validateAttribute
                    : validateAttribute;

                return this.GetErrorResult(string.Join(",", errors.ToArray()), attributeValue);
            }
        }
    }
}


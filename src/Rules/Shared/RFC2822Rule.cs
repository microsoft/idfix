using IdFix.Settings;
using System.DirectoryServices.Protocols;
using System.Text.RegularExpressions;

namespace IdFix.Rules.Shared
{
    class RFC2822Rule : Rule
    {
        public RFC2822Rule() : base() { }

        public override RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue)
        {
            var tldList = new ValidTLDList();
            bool success = true;
            ErrorType errors = ErrorType.None;
            string validateAttribute = Constants.SMTPRegex.IsMatch(attributeValue) ? attributeValue.Substring(attributeValue.IndexOf(":") + 1) : attributeValue;

            if (validateAttribute.LastIndexOf(".") != -1)
            {
                string tldDomain = validateAttribute.ToLowerInvariant().Substring(validateAttribute.LastIndexOf("."));
                if (tldDomain.Length > 1)
                {
                    if (!tldList.Contains(tldDomain))
                    {
                        success = false;
                        errors |= ErrorType.TopLevelDomain;
                    }
                }
            }

            if (!Constants.DomainPartRegex.IsMatch(validateAttribute))
            {
                success = false;
                errors |= ErrorType.DomainPart;
                if (validateAttribute.LastIndexOf("@") != -1)
                {
                    validateAttribute = validateAttribute.Substring(0, validateAttribute.LastIndexOf("@") + 1)
                        + (new Regex(@"[^a-z0-9.-]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Replace(validateAttribute.Substring(validateAttribute.LastIndexOf("@") + 1), "");
                }
            }

            if (!Constants.LocalPartRegex.IsMatch(validateAttribute) || (validateAttribute.Split('@').Length - 1 > 1))
            {
                success = false;
                errors |= ErrorType.LocalPart;
                if (validateAttribute.LastIndexOf("@") != -1)
                {
                    string validateLocal = (new Regex(@"[^a-z0-9.!#$%&'*+/=?^_`{|}~-]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Replace(validateAttribute.Substring(0, validateAttribute.LastIndexOf("@")), "");
                    validateAttribute = (validateLocal + validateAttribute.Substring(validateAttribute.LastIndexOf("@"))).Replace(".@", "@");
                }
            }

            if (!Constants.Rfc2822Regex.IsMatch(validateAttribute))
            {
                success = false;
                errors |= ErrorType.Format;
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

                return this.GetErrorResult(errors, attributeValue);
            }
        }
    }
}


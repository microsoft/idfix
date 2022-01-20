using IdFix.Rules.Shared;
using System;
using System.DirectoryServices.Protocols;

namespace IdFix.Rules.Dedicated
{
    /// <summary>
    /// Rule to ensure that TargetAddress of a group is not blank
    /// </summary>
    class TargetAddressBlankRule : BlankStringRule
    {
        /// <summary>
        /// Creates a new instance of <see cref="TargetAddressBlankRule"/>
        /// </summary>
        public TargetAddressBlankRule() : base((entry, value) =>
         {
             var objectType = ComposedRule.GetObjectType(entry);
            // the orginal code doesn't provide a fix if the type isn't "contact"
            if (objectType.Equals("contact", StringComparison.CurrentCultureIgnoreCase))
             {
                 value = "SMTP:" + entry.Attributes[StringLiterals.Mail][0].ToString();
                 return value.Length > 256 ? value.Substring(0, 256) : value;
             }

             return value;
         })
        { }

        /// <summary>
        /// Executes implementation for this rule
        /// </summary>
        /// <param name="parent">The composed rule containing this rule</param>
        /// <param name="entry">The search result we are checking</param>
        /// <param name="attributeValue">The current attribute value as pass through the chain</param>
        /// <returns>Either a success or error result</returns>
        public override RuleResult Execute(ComposedRule parent, SearchResultEntry entry, string attributeValue)
        {
            string objectType = this.GetObjectType(entry);
            var homeMdb = entry.Attributes[StringLiterals.HomeMdb];

            // When the entry is a contact or user
            if (objectType.Equals("contact", StringComparison.CurrentCultureIgnoreCase) || objectType.Equals("user", StringComparison.CurrentCultureIgnoreCase))
            {
                // and homeMdb is not defined 
                if (homeMdb == null || homeMdb.Count == 0 || homeMdb[0] == null || string.IsNullOrWhiteSpace(homeMdb[0].ToString()))
                {
                    // targetAddress cannot be blank
                    return base.Execute(parent, entry, attributeValue);
                }
            }

            return this.GetSuccessResult();
        }
    }
}

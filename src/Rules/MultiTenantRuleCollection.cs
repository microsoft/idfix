using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdFix.Rules
{
    class MultiTenantRuleCollection : RuleCollection
    {
        public override string[] AttributesToQuery => new string[] {
            StringLiterals.Cn,
            StringLiterals.DistinguishedName,
            StringLiterals.GroupType,
            StringLiterals.HomeMdb,
            StringLiterals.IsCriticalSystemObject,
            StringLiterals.MailNickName,
            StringLiterals.MsExchHideFromAddressLists,
            StringLiterals.MsExchRecipientTypeDetails,
            StringLiterals.ObjectClass,
            StringLiterals.ProxyAddresses,
            StringLiterals.SamAccountName,
            StringLiterals.TargetAddress,
            StringLiterals.UserPrincipalName
        };


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdFix.Settings
{
    enum RuleMode
    {
        MultiTenant,
        Dedicated
    }

    enum DirectoryType
    {
        ActiveDirectory,
        LDAP
    }

    enum CredentialMode
    {
        CurrentUser,
        Specified
    }
}

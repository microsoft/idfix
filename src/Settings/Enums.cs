using System;

namespace IdFix.Settings
{
    public enum RuleMode
    {
        MultiTenant,
        Dedicated
    }

    public enum DirectoryType
    {
        ActiveDirectory,
        LDAP
    }

    public enum CredentialMode
    {
        CurrentUser,
        Specified
    }

    [Flags]
    public enum ErrorType : uint
    {
        None = 0,
        Format = 1,
        Blank = 2,
        Duplicate = 4,
        Character = 8,
        TopLevelDomain = 16,
        DomainPart = 32,
        LocalPart = 64,
        Length = 128
    }
}

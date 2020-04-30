using System;

namespace IdFix.Settings
{
    /// <summary>
    /// Set of available rule modes
    /// </summary>
    public enum RuleMode
    {
        MultiTenant,
        Dedicated
    }

    /// <summary>
    /// The type of directory we are querying
    /// </summary>
    public enum DirectoryType
    {
        ActiveDirectory,
        LDAP
    }

    /// <summary>
    /// Indicates a credential mode for establising our connections
    /// </summary>
    public enum CredentialMode
    {
        CurrentUser,
        Specified
    }

    /// <summary>
    /// Types of errors we find
    /// </summary>
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

    /// <summary>
    /// Types of actions we can recommend or the user can take
    /// </summary>
    public enum ActionType
    {
        None,
        Complete,
        Edit,
        Remove,
        Fail,
        Undo
    }
}

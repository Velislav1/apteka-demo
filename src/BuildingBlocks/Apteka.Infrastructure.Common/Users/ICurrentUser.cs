namespace Apteka.Infrastructure.Common.Users;

/// <summary>The user on whose behalf data is changed. Written into every audit row.</summary>
public interface ICurrentUser
{
    /// <summary>Null for the system (seed, background work) until the Identity module provides real users.</summary>
    Guid? UserId { get; }

    string UserName { get; }
}

/// <summary>Used until login exists (Identity module), and for seed data.</summary>
public sealed class SystemUser : ICurrentUser
{
    public Guid? UserId => null;

    public string UserName => "system";
}

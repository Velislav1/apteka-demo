using Apteka.SharedKernel;

namespace Apteka.Infrastructure.Common.Users;

/// <summary>
/// Holds who is working in the current scope. The Host sets it from the login (cookie) for every request
/// and Blazor circuit; start-up work (migrations, seed data) runs as <see cref="SystemUser"/>.
/// Nobody set means <see cref="AnonymousUser"/>: no id and no rights.
/// </summary>
public sealed class CurrentUserAccessor
{
    public ICurrentUser User { get; set; } = AnonymousUser.Instance;
}

/// <summary>The <see cref="ICurrentUser"/> that services receive: always reads the accessor, so later changes are seen.</summary>
internal sealed class ScopedCurrentUser(CurrentUserAccessor accessor) : ICurrentUser
{
    public Guid? UserId => accessor.User.UserId;

    public string UserName => accessor.User.UserName;

    public bool IsInRole(string role) => accessor.User.IsInRole(role);
}

/// <summary>Start-up work and seed data. Allowed everything; written as „system“ in the audit log.</summary>
public sealed class SystemUser : ICurrentUser
{
    public static readonly SystemUser Instance = new();

    public Guid? UserId => null;

    public string UserName => "system";

    public bool IsInRole(string role) => true;
}

public sealed class AnonymousUser : ICurrentUser
{
    public static readonly AnonymousUser Instance = new();

    public Guid? UserId => null;

    public string UserName => "anonymous";

    public bool IsInRole(string role) => false;
}

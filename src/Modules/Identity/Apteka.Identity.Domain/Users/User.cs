using System.Text.RegularExpressions;
using Apteka.SharedKernel;

namespace Apteka.Identity.Domain.Users;

/// <summary>
/// A person who logs in. Never deleted, only deactivated (rule 6).
/// After <see cref="MaxFailedLogins"/> wrong passwords in a row the user is locked for <see cref="LockoutDuration"/>.
/// </summary>
public sealed partial class User : Entity<Guid>
{
    public const int MaxFailedLogins = 5;
    public const int MaxDisplayNameLength = 100;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

    private User()
    {
        UserName = string.Empty;
        DisplayName = string.Empty;
        PasswordHash = string.Empty;
    }

    private User(Guid id)
        : base(id)
    {
        UserName = string.Empty;
        DisplayName = string.Empty;
        PasswordHash = string.Empty;
    }

    /// <summary>Lower case; unique.</summary>
    public string UserName { get; private set; }

    public string DisplayName { get; private set; }

    public UserRole Role { get; private set; }

    /// <summary>Only the hash is stored; it is never written to the audit log.</summary>
    [AuditExcluded]
    public string PasswordHash { get; private set; }

    public bool IsActive { get; private set; }

    public int FailedLoginCount { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public static string NormalizeUserName(string userName) => userName.Trim().ToLowerInvariant();

    public static Result<User> Create(string userName, string displayName, UserRole role, string passwordHash)
    {
        var normalized = NormalizeUserName(userName);
        if (!UserNamePattern().IsMatch(normalized))
        {
            return Result.Failure<User>(UserErrors.InvalidUserName);
        }

        var user = new User(Guid.CreateVersion7()) { UserName = normalized, PasswordHash = passwordHash };
        var result = user.Update(displayName, role, isActive: true);
        return result.IsSuccess ? Result.Success(user) : Result.Failure<User>(result.Error);
    }

    public Result Update(string displayName, UserRole role, bool isActive)
    {
        var trimmed = displayName.Trim();
        if (trimmed.Length is 0 or > MaxDisplayNameLength)
        {
            return Result.Failure(UserErrors.DisplayNameRequired);
        }

        if (!Enum.IsDefined(role))
        {
            return Result.Failure(UserErrors.InvalidRole);
        }

        DisplayName = trimmed;
        Role = role;
        IsActive = isActive;
        return Result.Success();
    }

    /// <summary>A new password also lifts a lockout.</summary>
    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        FailedLoginCount = 0;
        LockedUntil = null;
    }

    public bool IsLockedOut(DateTimeOffset now) => LockedUntil > now;

    public void RegisterFailedLogin(DateTimeOffset now)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= MaxFailedLogins)
        {
            LockedUntil = now + LockoutDuration;
            FailedLoginCount = 0;
        }
    }

    public void RegisterSuccessfulLogin(DateTimeOffset now)
    {
        FailedLoginCount = 0;
        LockedUntil = null;
        LastLoginAt = now;
    }

    [GeneratedRegex("^[a-z0-9._-]{3,50}$", RegexOptions.CultureInvariant)]
    private static partial Regex UserNamePattern();
}

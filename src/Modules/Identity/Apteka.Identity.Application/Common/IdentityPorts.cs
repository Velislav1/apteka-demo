using Apteka.Identity.Domain.Users;
using Apteka.SharedKernel;

namespace Apteka.Identity.Application.Common;

/// <summary>Hashes and checks passwords. Implemented in Infrastructure with ASP.NET Core's PasswordHasher (PBKDF2).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);
}

public interface IUserRepository
{
    Task<User?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> FindByUserNameAsync(string normalizedUserName, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserView>> ListAsync(CancellationToken cancellationToken);

    Task<int> CountActiveManagersAsync(CancellationToken cancellationToken);

    void Add(User user);

    /// <summary>Saves; a user name taken by a concurrent save is reported as an error, not an exception.</summary>
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record UserView(Guid Id, string UserName, string DisplayName, UserRole Role, bool IsActive, DateTimeOffset? LastLoginAt);

using Apteka.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Apteka.Identity.Infrastructure.Users;

/// <summary>ASP.NET Core Identity's hasher: PBKDF2 with a random salt per password; old hashes still verify after upgrades.</summary>
internal sealed class PasswordHasher : Application.Common.IPasswordHasher
{
    private static readonly User Owner = null!;
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(Owner, password);

    public bool Verify(string passwordHash, string password) =>
        _inner.VerifyHashedPassword(Owner, passwordHash, password) != PasswordVerificationResult.Failed;
}

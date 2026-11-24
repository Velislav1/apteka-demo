using System.Security.Cryptography;
using Apteka.Identity.Application.Common;
using Apteka.Identity.Domain.Users;
using Apteka.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Apteka.Identity.Infrastructure.Seed;

/// <summary>
/// Two demo users, „manager“ and „pharmacist“. Passwords are never in git (rule 8): they come from
/// configuration (appsettings.Development.json, section Identity:Seed); if missing, a random one is
/// generated and written to the console once.
/// </summary>
internal sealed class IdentitySeeder(
    IdentityDbContext context,
    IPasswordHasher hasher,
    IdentitySeedPasswords passwords,
    ILogger<IdentitySeeder> logger) : IModuleDatabaseInitializer
{
    public int Order => 5;

    public Task MigrateAsync(CancellationToken cancellationToken) => context.Database.MigrateAsync(cancellationToken);

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        Add("manager", "Управител (демо)", UserRole.Manager, passwords.ManagerPassword);
        Add("pharmacist", "Фармацевт (демо)", UserRole.Pharmacist, passwords.PharmacistPassword);
        await context.SaveChangesAsync(cancellationToken);
    }

    private void Add(string userName, string displayName, UserRole role, string? configured)
    {
        var password = string.IsNullOrWhiteSpace(configured) ? Generate() : configured;
        if (string.IsNullOrWhiteSpace(configured))
        {
            logger.LogWarning(
                "Demo user {UserName} was created with the generated password {Password}. Set Identity:Seed in appsettings.Development.json to choose your own.",
                userName,
                password);
        }

        if (!PasswordPolicy.IsStrongEnough(password))
        {
            throw new InvalidOperationException($"The configured password for {userName} is too weak (at least {PasswordPolicy.MinLength} characters with a letter and a digit).");
        }

        context.Users.Add(User.Create(userName, displayName, role, hasher.Hash(password)).Value);
    }

    private static string Generate()
    {
        const string letters = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        var chars = Enumerable.Range(0, 10).Select(i => i % 3 == 2
            ? digits[RandomNumberGenerator.GetInt32(digits.Length)]
            : letters[RandomNumberGenerator.GetInt32(letters.Length)]);
        return new string([.. chars]);
    }
}

/// <summary>Demo passwords from configuration (Identity:Seed:ManagerPassword / PharmacistPassword).</summary>
public sealed record IdentitySeedPasswords(string? ManagerPassword, string? PharmacistPassword);

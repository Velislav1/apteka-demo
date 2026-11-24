using Apteka.Identity.Application.Common;
using Apteka.Identity.Domain.Users;
using Apteka.SharedKernel;

namespace Apteka.Identity.Application.Login;

/// <summary>Use case „Вход“: checks the password, counts wrong attempts and locks the user after too many.</summary>
public sealed class LoginHandler(IUserRepository users, IPasswordHasher hasher, TimeProvider clock)
{
    public async Task<Result<AuthenticatedUser>> HandleAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByUserNameAsync(User.NormalizeUserName(userName), cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthenticatedUser>(UserErrors.InvalidCredentials);
        }

        var now = clock.GetUtcNow();
        if (user.IsLockedOut(now))
        {
            return Result.Failure<AuthenticatedUser>(UserErrors.LockedOut(user.LockedUntil!.Value));
        }

        if (!hasher.Verify(user.PasswordHash, password))
        {
            user.RegisterFailedLogin(now);
            await users.SaveChangesAsync(cancellationToken);
            return Result.Failure<AuthenticatedUser>(
                user.IsLockedOut(now) ? UserErrors.LockedOut(user.LockedUntil!.Value) : UserErrors.InvalidCredentials);
        }

        user.RegisterSuccessfulLogin(now);
        await users.SaveChangesAsync(cancellationToken);
        return Result.Success(new AuthenticatedUser(user.Id, user.UserName, user.DisplayName, user.Role));
    }
}

/// <summary>What goes into the login cookie.</summary>
public sealed record AuthenticatedUser(Guid Id, string UserName, string DisplayName, UserRole Role)
{
    public string RoleName => Role.ToString();
}

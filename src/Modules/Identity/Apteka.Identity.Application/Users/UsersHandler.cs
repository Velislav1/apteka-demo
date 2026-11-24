using Apteka.Identity.Application.Common;
using Apteka.Identity.Contracts;
using Apteka.Identity.Domain.Users;
using Apteka.SharedKernel;

namespace Apteka.Identity.Application.Users;

/// <summary>Use cases of the „Потребители“ screen. Only a manager may use them, and one active manager must always remain.</summary>
/// <remarks>Changes run in <see cref="IUnitOfWork"/>: a refused change is rolled back and forgotten.</remarks>
public sealed class UsersHandler(IUserRepository users, IPasswordHasher hasher, ICurrentUser currentUser, IUnitOfWork unitOfWork)
{
    public Task<IReadOnlyList<UserView>> ListAsync(CancellationToken cancellationToken = default) => users.ListAsync(cancellationToken);

    public async Task<Result<Guid>> CreateAsync(NewUserInput input, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsInRole(Roles.Manager))
        {
            return Result.Failure<Guid>(UserErrors.ManagersOnly);
        }

        if (!PasswordPolicy.IsStrongEnough(input.Password))
        {
            return Result.Failure<Guid>(UserErrors.WeakPassword);
        }

        var created = User.Create(input.UserName, input.DisplayName, input.Role, hasher.Hash(input.Password));
        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        if (await users.FindByUserNameAsync(created.Value.UserName, cancellationToken) is not null)
        {
            return Result.Failure<Guid>(UserErrors.UserNameTaken(created.Value.UserName));
        }

        users.Add(created.Value);
        var saved = await users.SaveChangesAsync(cancellationToken);
        return saved.IsSuccess ? Result.Success(created.Value.Id) : Result.Failure<Guid>(saved.Error);
    }

    public Task<Result> UpdateAsync(Guid id, string displayName, UserRole role, bool isActive, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(ct => UpdateCoreAsync(id, displayName, role, isActive, ct), cancellationToken);

    public Task<Result> SetPasswordAsync(Guid id, string password, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(ct => SetPasswordCoreAsync(id, password, ct), cancellationToken);

    private async Task<Result> UpdateCoreAsync(Guid id, string displayName, UserRole role, bool isActive, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(Roles.Manager))
        {
            return Result.Failure(UserErrors.ManagersOnly);
        }

        var user = await users.GetAsync(id, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound);
        }

        var wasActiveManager = user is { IsActive: true, Role: UserRole.Manager };
        var updated = user.Update(displayName, role, isActive);
        if (updated.IsFailure)
        {
            return updated;
        }

        var staysActiveManager = user is { IsActive: true, Role: UserRole.Manager };
        if (wasActiveManager && !staysActiveManager && await users.CountActiveManagersAsync(cancellationToken) <= 1)
        {
            return Result.Failure(UserErrors.LastManager);
        }

        return await users.SaveChangesAsync(cancellationToken);
    }

    private async Task<Result> SetPasswordCoreAsync(Guid id, string password, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(Roles.Manager))
        {
            return Result.Failure(UserErrors.ManagersOnly);
        }

        if (!PasswordPolicy.IsStrongEnough(password))
        {
            return Result.Failure(UserErrors.WeakPassword);
        }

        var user = await users.GetAsync(id, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound);
        }

        user.SetPasswordHash(hasher.Hash(password));
        return await users.SaveChangesAsync(cancellationToken);
    }
}

public sealed record NewUserInput(string UserName, string DisplayName, UserRole Role, string Password);

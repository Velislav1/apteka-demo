using Apteka.Identity.Application.Common;
using Apteka.Identity.Contracts;
using Apteka.Identity.Domain.Users;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Apteka.Identity.Infrastructure.Users;

internal sealed class UserRepository(IdentityDbContext context) : IUserRepository, IUserDirectory
{
    public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> FindByUserNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        context.Users.SingleOrDefaultAsync(u => u.UserName == normalizedUserName, cancellationToken);

    public async Task<IReadOnlyList<UserView>> ListAsync(CancellationToken cancellationToken) =>
        await context.Users.AsNoTracking()
            .OrderBy(u => u.UserName)
            .Select(u => new UserView(u.Id, u.UserName, u.DisplayName, u.Role, u.IsActive, u.LastLoginAt))
            .ToListAsync(cancellationToken);

    public Task<int> CountActiveManagersAsync(CancellationToken cancellationToken) =>
        context.Users.AsNoTracking().CountAsync(u => u.IsActive && u.Role == UserRole.Manager, cancellationToken);

    public void Add(User user) => context.Users.Add(user);

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: IdentityDbContext.UniqueUserNameIndex,
        })
        {
            var name = context.ChangeTracker.Entries<User>().Select(e => e.Entity.UserName).FirstOrDefault() ?? string.Empty;
            context.ChangeTracker.Clear();
            return Result.Failure(UserErrors.UserNameTaken(name));
        }
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.ToArray();
        return await context.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);
    }
}

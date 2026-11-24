using Apteka.Catalog.Application.Common;
using Apteka.Catalog.Domain.Common;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Apteka.Catalog.Infrastructure.Common;

/// <summary>EF storage for any catalog reference list (manufacturers, groups, shelves).</summary>
internal sealed class ReferenceRepository<T>(CatalogDbContext context) : IReferenceRepository<T>
    where T : CatalogReference
{
    public async Task<IReadOnlyList<ReferenceItemView>> ListAsync(bool includeInactive, CancellationToken cancellationToken) =>
        await context.Set<T>()
            .AsNoTracking()
            .Where(r => includeInactive || r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => new ReferenceItemView(r.Id, r.Name, r.IsActive))
            .ToListAsync(cancellationToken);

    public Task<T?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Set<T>().SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var lower = name.ToLower(System.Globalization.CultureInfo.CurrentCulture);
        return context.Set<T>().AnyAsync(r => r.Name.ToLower() == lower && r.Id != exceptId, cancellationToken);
    }

    public void Add(T item) => context.Set<T>().Add(item);

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            var name = context.ChangeTracker.Entries<T>()
                .Where(e => e.State is EntityState.Added or EntityState.Modified)
                .Select(e => e.Entity.Name)
                .FirstOrDefault() ?? string.Empty;
            context.ChangeTracker.Clear();
            return Result.Failure(ReferenceErrors.NameTaken(name));
        }
    }
}

using Apteka.Purchasing.Application.Suppliers;
using Apteka.Purchasing.Domain.Suppliers;
using Apteka.Purchasing.Infrastructure.Persistence.Configurations;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Apteka.Purchasing.Infrastructure.Suppliers;

internal sealed class SupplierRepository(PurchasingDbContext context) : ISupplierRepository
{
    public async Task<IReadOnlyList<SupplierView>> ListAsync(bool includeInactive, CancellationToken cancellationToken) =>
        await context.Suppliers.AsNoTracking()
            .Where(s => includeInactive || s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SupplierView(s.Id, s.Name, s.Eik, s.PaymentTermDays, s.IsActive))
            .ToListAsync(cancellationToken);

    public Task<Supplier?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Suppliers.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var lower = name.ToLower(System.Globalization.CultureInfo.CurrentCulture);
        return context.Suppliers.AnyAsync(s => s.Name.ToLower() == lower && s.Id != exceptId, cancellationToken);
    }

    public Task<bool> EikExistsAsync(string eik, Guid? exceptId, CancellationToken cancellationToken) =>
        context.Suppliers.AnyAsync(s => s.Eik == eik && s.Id != exceptId, cancellationToken);

    public void Add(Supplier supplier) => context.Suppliers.Add(supplier);

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            var supplier = context.ChangeTracker.Entries<Supplier>()
                .Where(e => e.State is EntityState.Added or EntityState.Modified)
                .Select(e => e.Entity)
                .FirstOrDefault();
            context.ChangeTracker.Clear();

            return Result.Failure(pg.ConstraintName == SupplierConfiguration.UniqueEikIndex
                ? SupplierErrors.EikTaken(supplier?.Eik ?? string.Empty)
                : SupplierErrors.NameTaken(supplier?.Name ?? string.Empty));
        }
    }
}

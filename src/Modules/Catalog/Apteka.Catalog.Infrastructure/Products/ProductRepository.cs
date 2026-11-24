using Apteka.Catalog.Application.Products;
using Apteka.Catalog.Domain.Products;
using Apteka.Catalog.Infrastructure.Persistence.Configurations;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Apteka.Catalog.Infrastructure.Products;

internal sealed class ProductRepository(CatalogDbContext context) : IProductRepository
{
    public Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Products
            .Include(p => p.Units)
            .Include(p => p.Barcodes)
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<string>> FindTakenBarcodesAsync(
        IEnumerable<string> codes,
        Guid? exceptProductId,
        CancellationToken cancellationToken)
    {
        var list = codes.ToList();
        if (list.Count == 0)
        {
            return [];
        }

        return await context.Barcodes
            .AsNoTracking()
            .Where(b => list.Contains(b.Code) && b.ProductId != exceptProductId)
            .Select(b => b.Code)
            .ToListAsync(cancellationToken);
    }

    public void Add(Product product) => context.Products.Add(product);

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
            ConstraintName: BarcodeConfiguration.UniqueCodeIndex,
        })
        {
            // Another user saved the same barcode a moment earlier.
            var pending = context.ChangeTracker.Entries<Barcode>()
                .Where(e => e.State is EntityState.Added or EntityState.Modified)
                .Select(e => (e.Entity.Code, e.Entity.ProductId))
                .ToList();
            context.ChangeTracker.Clear();

            var code = string.Empty;
            foreach (var (candidate, productId) in pending)
            {
                if ((await FindTakenBarcodesAsync([candidate], productId, cancellationToken)).Count > 0)
                {
                    code = candidate;
                    break;
                }
            }

            return Result.Failure(CatalogErrors.BarcodeTaken(code));
        }
    }
}

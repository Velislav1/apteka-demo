using Apteka.Catalog.Domain.Products;
using Apteka.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Catalog.Infrastructure.Seed;

internal sealed class CatalogDatabaseInitializer(CatalogDbContext context) : IModuleDatabaseInitializer
{
    public int Order => 10;

    public Task MigrateAsync(CancellationToken cancellationToken) => context.Database.MigrateAsync(cancellationToken);

    /// <summary>Adds the demo products once, only into an empty catalog.</summary>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await context.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        foreach (var (details, units, barcodes) in CatalogSeedData.Products())
        {
            var product = Product.Create(details, units, barcodes);
            if (product.IsFailure)
            {
                throw new InvalidOperationException($"Invalid seed product '{details.Name}': {product.Error.Message}");
            }

            context.Products.Add(product.Value);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

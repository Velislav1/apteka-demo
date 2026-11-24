using Apteka.Catalog.Domain.Common;
using Apteka.Catalog.Domain.Manufacturers;
using Apteka.Catalog.Domain.ProductGroups;
using Apteka.Catalog.Domain.Products;
using Apteka.Catalog.Domain.Shelves;
using Apteka.Infrastructure.Common.Persistence;
using Apteka.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Apteka.Catalog.Infrastructure.Seed;

internal sealed class CatalogDatabaseInitializer(CatalogDbContext context) : IModuleDatabaseInitializer
{
    public int Order => 10;

    public Task MigrateAsync(CancellationToken cancellationToken) => context.Database.MigrateAsync(cancellationToken);

    /// <summary>Adds the demo lists and products; each list only if it is still empty.</summary>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var manufacturers = await EnsureAsync(context.Manufacturers, CatalogSeedData.Manufacturers, Manufacturer.Create, cancellationToken);
        var groups = await EnsureAsync(context.ProductGroups, CatalogSeedData.Groups, ProductGroup.Create, cancellationToken);
        var shelves = await EnsureAsync(context.Shelves, CatalogSeedData.Shelves, Shelf.Create, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        if (await context.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        foreach (var seed in CatalogSeedData.Products())
        {
            var details = seed.ToDetails(manufacturers[seed.Manufacturer], groups[seed.Group], shelves[seed.Shelf]);
            var product = Product.Create(details, seed.Units, seed.Barcodes);
            if (product.IsFailure)
            {
                throw new InvalidOperationException($"Invalid seed product '{seed.Name}': {product.Error.Message}");
            }

            context.Products.Add(product.Value);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Returns name → id for the seed names, adding the ones that are missing.</summary>
    private static async Task<Dictionary<string, Guid>> EnsureAsync<T>(
        DbSet<T> set,
        IEnumerable<string> names,
        Func<string, Result<T>> create,
        CancellationToken cancellationToken)
        where T : CatalogReference
    {
        var existing = await set.ToDictionaryAsync(r => r.Name, r => r.Id, cancellationToken);

        foreach (var name in names.Where(n => !existing.ContainsKey(n)))
        {
            var item = create(name).Value;
            set.Add(item);
            existing[name] = item.Id;
        }

        return existing;
    }
}

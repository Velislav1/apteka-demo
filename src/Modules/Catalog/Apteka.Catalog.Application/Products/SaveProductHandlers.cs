using Apteka.Catalog.Application.Common;
using Apteka.Catalog.Domain.Common;
using Apteka.Catalog.Domain.Manufacturers;
using Apteka.Catalog.Domain.ProductGroups;
using Apteka.Catalog.Domain.Products;
using Apteka.Catalog.Domain.Shelves;
using Apteka.SharedKernel;

namespace Apteka.Catalog.Application.Products;

/// <summary>Use case „Нова стока“.</summary>
public sealed class CreateProductHandler(IProductRepository products, ProductReferenceRules references)
{
    public async Task<Result<Guid>> HandleAsync(ProductInput input, CancellationToken cancellationToken = default)
    {
        var created = Product.Create(input.ToDetails(), input.ToUnitSpecs(), input.ToBarcodeSpecs());
        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        var product = created.Value;
        var available = await references.CheckAsync(product, previous: null, cancellationToken);
        if (available.IsFailure)
        {
            return Result.Failure<Guid>(available.Error);
        }

        var taken = await BarcodeRules.CheckNotTakenAsync(products, product, cancellationToken);
        if (taken.IsFailure)
        {
            return Result.Failure<Guid>(taken.Error);
        }

        products.Add(product);
        var saved = await products.SaveChangesAsync(cancellationToken);

        return saved.IsSuccess ? Result.Success(product.Id) : Result.Failure<Guid>(saved.Error);
    }
}

/// <summary>Use case „Редакция на стока“.</summary>
/// <remarks>Runs in <see cref="IUnitOfWork"/>: if a later check fails, the half-applied change is rolled back and forgotten.</remarks>
public sealed class UpdateProductHandler(IProductRepository products, ProductReferenceRules references, IUnitOfWork unitOfWork)
{
    public Task<Result> HandleAsync(Guid id, ProductInput input, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteAsync(ct => UpdateAsync(id, input, ct), cancellationToken);

    private async Task<Result> UpdateAsync(Guid id, ProductInput input, CancellationToken cancellationToken)
    {
        var product = await products.GetAsync(id, cancellationToken);
        if (product is null)
        {
            return Result.Failure(CatalogErrors.ProductNotFound);
        }

        var previous = new ProductReferences(product.ManufacturerId, product.ProductGroupId, product.ShelfId);
        var updated = product.Update(input.ToDetails(), input.ToUnitSpecs(), input.ToBarcodeSpecs());
        if (updated.IsFailure)
        {
            return updated;
        }

        var available = await references.CheckAsync(product, previous, cancellationToken);
        if (available.IsFailure)
        {
            return available;
        }

        var taken = await BarcodeRules.CheckNotTakenAsync(products, product, cancellationToken);
        if (taken.IsFailure)
        {
            return taken;
        }

        return await products.SaveChangesAsync(cancellationToken);
    }
}

internal static class BarcodeRules
{
    /// <summary>
    /// Friendly early check that no other product uses these barcodes.
    /// The unique index in the database is the real guarantee (two users saving at the same time).
    /// </summary>
    public static async Task<Result> CheckNotTakenAsync(IProductRepository products, Product product, CancellationToken cancellationToken)
    {
        var taken = await products.FindTakenBarcodesAsync(product.Barcodes.Select(b => b.Code), product.Id, cancellationToken);

        return taken.Count == 0 ? Result.Success() : Result.Failure(CatalogErrors.BarcodeTaken(taken[0]));
    }
}

internal sealed record ProductReferences(Guid? ManufacturerId, Guid? ProductGroupId, Guid? ShelfId);

/// <summary>
/// A product may point only to an existing, active manufacturer, group and shelf.
/// A reference that did not change is kept even if that item was deactivated later.
/// </summary>
public sealed class ProductReferenceRules(
    IReferenceRepository<Manufacturer> manufacturers,
    IReferenceRepository<ProductGroup> groups,
    IReferenceRepository<Shelf> shelves)
{
    internal async Task<Result> CheckAsync(Product product, ProductReferences? previous, CancellationToken cancellationToken)
    {
        if (!await IsAvailableAsync(manufacturers, product.ManufacturerId, previous?.ManufacturerId, cancellationToken))
        {
            return Result.Failure(CatalogErrors.ManufacturerNotAvailable);
        }

        if (!await IsAvailableAsync(groups, product.ProductGroupId, previous?.ProductGroupId, cancellationToken))
        {
            return Result.Failure(CatalogErrors.ProductGroupNotAvailable);
        }

        return await IsAvailableAsync(shelves, product.ShelfId, previous?.ShelfId, cancellationToken)
            ? Result.Success()
            : Result.Failure(CatalogErrors.ShelfNotAvailable);
    }

    private static async Task<bool> IsAvailableAsync<T>(
        IReferenceRepository<T> items,
        Guid? id,
        Guid? previousId,
        CancellationToken cancellationToken)
        where T : CatalogReference
    {
        if (id is null || id == previousId)
        {
            return true;
        }

        var item = await items.GetAsync(id.Value, cancellationToken);
        return item is { IsActive: true };
    }
}

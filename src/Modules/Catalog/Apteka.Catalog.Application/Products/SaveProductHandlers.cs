using Apteka.Catalog.Domain.Products;
using Apteka.SharedKernel;

namespace Apteka.Catalog.Application.Products;

/// <summary>Use case „Нова стока“.</summary>
public sealed class CreateProductHandler(IProductRepository products)
{
    public async Task<Result<Guid>> HandleAsync(ProductInput input, CancellationToken cancellationToken = default)
    {
        var created = Product.Create(input.ToDetails(), input.ToUnitSpecs(), input.ToBarcodeSpecs());
        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        var product = created.Value;
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
public sealed class UpdateProductHandler(IProductRepository products)
{
    public async Task<Result> HandleAsync(Guid id, ProductInput input, CancellationToken cancellationToken = default)
    {
        var product = await products.GetAsync(id, cancellationToken);
        if (product is null)
        {
            return Result.Failure(CatalogErrors.ProductNotFound);
        }

        var updated = product.Update(input.ToDetails(), input.ToUnitSpecs(), input.ToBarcodeSpecs());
        if (updated.IsFailure)
        {
            return updated;
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

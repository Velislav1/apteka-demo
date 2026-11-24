using Apteka.Catalog.Contracts;
using Apteka.Inventory.Contracts;
using Apteka.Sales.Domain.Pricing;
using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;

namespace Apteka.Sales.Application.Checkout;

/// <summary>Barcode (or name) at the till → product, unit, price and stock. Nothing is reserved yet.</summary>
public sealed class FindTillProductHandler(ICatalogLookup catalog, IStockQueries stock, TimeProvider clock)
{
    public static readonly Error UnknownBarcode = new("sales.unknown_barcode", "Непознат баркод.");

    public static Error NoMatch(string text) => new("sales.no_match", $"Няма стока „{text}“.");

    public async Task<Result<TillProduct>> ByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var unit = await catalog.FindByBarcodeAsync(barcode.Trim(), cancellationToken);
        if (unit is null)
        {
            return Result.Failure<TillProduct>(UnknownBarcode);
        }

        var product = await catalog.GetProductAsync(unit.ProductId, cancellationToken);
        return product is null
            ? Result.Failure<TillProduct>(UnknownBarcode)
            : await PriceAsync(product, unit.UnitId, cancellationToken);
    }

    /// <summary>The product with this code (номенклатурен номер), in its default sale unit.</summary>
    public async Task<Result<TillProduct>> ByCodeAsync(long code, CancellationToken cancellationToken = default)
    {
        var product = await catalog.FindByCodeAsync(code, cancellationToken);
        return product is null
            ? Result.Failure<TillProduct>(UnknownCode(code))
            : await PriceAsync(product, product.DefaultSaleUnitId, cancellationToken);
    }

    public static Error UnknownCode(long code) => new("sales.unknown_code", $"Няма стока с код {code}.");

    /// <summary>Active products whose name contains the text, each in its default sale unit.</summary>
    public async Task<Result<IReadOnlyList<TillProduct>>> ByNameAsync(string text, int limit, CancellationToken cancellationToken = default)
    {
        var products = await catalog.SearchProductsAsync(text, limit, cancellationToken);
        var found = new List<TillProduct>();
        foreach (var product in products)
        {
            if (await PriceAsync(product, product.DefaultSaleUnitId, cancellationToken) is { IsSuccess: true } priced)
            {
                found.Add(priced.Value);
            }
        }

        return found.Count == 0
            ? Result.Failure<IReadOnlyList<TillProduct>>(NoMatch(text))
            : Result.Success<IReadOnlyList<TillProduct>>(found);
    }

    public async Task<Result<TillProduct>> InUnitAsync(Guid productId, Guid unitId, CancellationToken cancellationToken = default)
    {
        var product = await catalog.GetProductAsync(productId, cancellationToken);
        return product is null
            ? Result.Failure<TillProduct>(SalesErrors.ProductNotAvailable("?"))
            : await PriceAsync(product, unitId, cancellationToken);
    }

    private async Task<Result<TillProduct>> PriceAsync(CatalogProductInfo product, Guid? unitId, CancellationToken cancellationToken)
    {
        if (!product.IsActive)
        {
            return Result.Failure<TillProduct>(SalesErrors.ProductNotAvailable(product.Name));
        }

        if (product.DispensingMode == CatalogDispensingMode.Narcotic)
        {
            return Result.Failure<TillProduct>(SalesErrors.NarcoticNotAllowed(product.Name));
        }

        var unit = product.Units.FirstOrDefault(u => u.UnitId == unitId) ?? product.Units.MaxBy(u => u.Factor)!;
        var today = BulgarianTime.Today(clock.GetUtcNow());
        var sellable = (await stock.GetBatchesAsync(product.ProductId, null, cancellationToken))
            .Where(b => b.ExpiryDate >= today)
            .ToList();

        if (sellable.Count == 0)
        {
            return Result.Failure<TillProduct>(SalesErrors.NotEnoughStock(product.Name, "няма наличност."));
        }

        var first = sellable[0];
        return Result.Success(new TillProduct(
            product.ProductId,
            product.Name,
            product.DispensingMode,
            product.VatRate,
            unit.UnitId,
            unit.Name,
            unit.Factor,
            SalePricing.UnitPrice(unit.Factor, first.PricedUnitFactor, first.PricedUnitSalePrice, first.SalePricePerBaseUnit),
            sellable.Sum(b => b.BaseQuantity),
            product.ShelfName,
            product.Units));
    }
}

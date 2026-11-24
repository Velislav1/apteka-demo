namespace Apteka.Catalog.Contracts;

/// <summary>
/// What other modules (Purchasing, Sales) may ask the catalog. They never see Catalog's tables or entities.
/// </summary>
public interface ICatalogLookup
{
    /// <summary>The product and unit a scanned barcode belongs to, or null if the code is unknown.</summary>
    Task<CatalogUnitInfo?> FindByBarcodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>A product with all its units, or null.</summary>
    Task<CatalogProductInfo?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
}

public sealed record CatalogProductInfo(
    Guid ProductId,
    string Name,
    decimal VatRate,
    bool IsRx,
    bool IsActive,
    IReadOnlyList<CatalogUnit> Units);

public sealed record CatalogUnit(Guid UnitId, string Name, int Factor);

public sealed record CatalogUnitInfo(
    Guid ProductId,
    string ProductName,
    decimal VatRate,
    bool IsRx,
    bool IsActive,
    Guid UnitId,
    string UnitName,
    int Factor);

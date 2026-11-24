namespace Apteka.Catalog.Contracts;

/// <summary>
/// What other modules (Purchasing, Sales) may ask the catalog. They never see Catalog's tables or entities.
/// </summary>
public interface ICatalogLookup
{
    /// <summary>The product and unit a scanned barcode belongs to, or null if the code is unknown.</summary>
    Task<CatalogUnitInfo?> FindByBarcodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>The product with this code (номенклатурен номер), or null.</summary>
    Task<CatalogProductInfo?> FindByCodeAsync(long code, CancellationToken cancellationToken = default);

    /// <summary>A product with all its units, or null.</summary>
    Task<CatalogProductInfo?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>The given products with their units (unknown ids are left out).</summary>
    Task<IReadOnlyList<CatalogProductInfo>> GetProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default);

    /// <summary>Active products whose name contains <paramref name="text"/> (case-insensitive), by name.</summary>
    Task<IReadOnlyList<CatalogProductInfo>> SearchProductsAsync(string text, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Active products for reports, by name: of one group (null = all) and, with <paramref name="withMinStockOnly"/>,
    /// only those that have a minimum stock set („Под минимален запас“); <paramref name="shelfId"/> keeps one shelf.
    /// </summary>
    Task<IReadOnlyList<CatalogProductInfo>> ListProductsAsync(Guid? groupId, bool withMinStockOnly, CancellationToken cancellationToken = default, Guid? shelfId = null);

    /// <summary>Active product groups by name, for report filters.</summary>
    Task<IReadOnlyList<CatalogGroup>> ListGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>Active shelves by name (e.g. for „Инвентаризация“ of one shelf).</summary>
    Task<IReadOnlyList<CatalogShelf>> ListShelvesAsync(CancellationToken cancellationToken = default);
}

public sealed record CatalogGroup(Guid Id, string Name);

public sealed record CatalogShelf(Guid Id, string Name);

public sealed record CatalogProductInfo(
    Guid ProductId,
    string Name,
    decimal VatRate,
    CatalogDispensingMode DispensingMode,
    bool IsActive,
    Guid? DefaultSaleUnitId,
    IReadOnlyList<CatalogUnit> Units,
    string? ShelfName = null,
    long Code = 0,
    decimal MarkupPercent = 25m,
    Guid? ProductGroupId = null,
    int? MinStock = null,
    int? MaxStock = null);

public static class CatalogCodes
{
    /// <summary>A short number (up to 7 digits) is a product code; barcodes have 8 or more digits.</summary>
    public static bool LooksLikeCode(string text) => text.Length is > 0 and < 8 && text.All(char.IsAsciiDigit);
}

public sealed record CatalogUnit(Guid UnitId, string Name, int Factor);

public sealed record CatalogUnitInfo(
    Guid ProductId,
    string ProductName,
    decimal VatRate,
    CatalogDispensingMode DispensingMode,
    bool IsActive,
    Guid UnitId,
    string UnitName,
    int Factor);

/// <summary>How a product may be dispensed. Same values as the catalog's own enum.</summary>
public enum CatalogDispensingMode
{
    OverTheCounter = 1,
    Prescription = 2,
    Narcotic = 3,
}

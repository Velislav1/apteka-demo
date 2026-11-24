using Apteka.Catalog.Domain.Products;

namespace Apteka.Catalog.Application.Products;

/// <summary>Read side for the screens. Implemented in Infrastructure with projections (no tracking).</summary>
public interface IProductQueries
{
    /// <summary>
    /// Products whose name contains <paramref name="text"/> (case-insensitive) or that have exactly this barcode.
    /// Empty text returns the first products by name.
    /// </summary>
    Task<IReadOnlyList<ProductListItem>> SearchAsync(string? text, int limit, CancellationToken cancellationToken);

    Task<ProductView?> GetAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record ProductListItem(
    Guid Id,
    string Name,
    string? Manufacturer,
    string? ProductGroup,
    decimal VatRate,
    DispensingMode DispensingMode,
    bool IsActive,
    string BaseUnitName,
    IReadOnlyList<string> Barcodes);

public sealed record ProductView(
    Guid Id,
    string Name,
    string? AtcCode,
    DispensingMode DispensingMode,
    decimal VatRate,
    bool IsActive,
    string? NhifCode,
    Guid? ManufacturerId,
    Guid? ProductGroupId,
    Guid? ShelfId,
    string? DefaultSaleUnitName,
    IReadOnlyList<ProductUnitView> Units,
    IReadOnlyList<BarcodeView> Barcodes);

public sealed record ProductUnitView(Guid Id, string Name, int Factor);

public sealed record BarcodeView(Guid Id, string Code, Guid ProductUnitId, string UnitName);

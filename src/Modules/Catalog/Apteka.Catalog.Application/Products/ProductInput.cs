using Apteka.Catalog.Domain.Products;

namespace Apteka.Catalog.Application.Products;

/// <summary>What the product form sends for create and update.</summary>
public sealed record ProductInput(
    string Name,
    string? AtcCode,
    string? Manufacturer,
    decimal VatRate,
    bool IsRx,
    bool IsActive,
    IReadOnlyList<UnitInput> Units,
    IReadOnlyList<BarcodeInput> Barcodes)
{
    internal ProductDetails ToDetails() => new(Name, AtcCode, Manufacturer, VatRate, IsRx, IsActive);

    internal List<UnitSpec> ToUnitSpecs() => Units.Select(u => new UnitSpec(u.Id, u.Name, u.Factor)).ToList();

    internal List<BarcodeSpec> ToBarcodeSpecs() => Barcodes.Select(b => new BarcodeSpec(b.Id, b.Code, b.UnitName)).ToList();
}

/// <param name="Id">Null for a new unit.</param>
public sealed record UnitInput(Guid? Id, string Name, int Factor);

/// <param name="Id">Null for a new barcode.</param>
/// <param name="UnitName">Name of one of the product's units.</param>
public sealed record BarcodeInput(Guid? Id, string Code, string UnitName);

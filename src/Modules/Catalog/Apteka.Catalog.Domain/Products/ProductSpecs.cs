namespace Apteka.Catalog.Domain.Products;

/// <summary>The editable fields of a product.</summary>
public sealed record ProductDetails(
    string Name,
    string? AtcCode,
    string? Manufacturer,
    decimal VatRate,
    bool IsRx,
    bool IsActive);

/// <summary>A unit as requested by the user. <paramref name="Id"/> is null for a new unit.</summary>
public sealed record UnitSpec(Guid? Id, string Name, int Factor);

/// <summary>
/// A barcode as requested by the user. <paramref name="Id"/> is null for a new barcode.
/// The unit is identified by name because new units have no id yet (names are unique within a product).
/// </summary>
public sealed record BarcodeSpec(Guid? Id, string Code, string UnitName);

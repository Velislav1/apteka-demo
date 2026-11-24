namespace Apteka.Catalog.Domain.Products;

/// <summary>The editable fields of a product.</summary>
/// <param name="NhifCode">The product's code in the NHIF (НЗОК) list, if it is reimbursed.</param>
/// <param name="DefaultSaleUnitName">Unit the till offers first; null means the largest unit.</param>
/// <param name="LatinName">Name in Latin letters (наименование лат.).</param>
/// <param name="GenericName">Generic / INN name (генерика), e.g. „Paracetamol“.</param>
/// <param name="MarkupPercent">Trade markup of this product; null means the group's (then the default).</param>
/// <param name="MinStock">Minimum stock in base units (for a „below minimum“ report later).</param>
/// <param name="MaxStock">Maximum stock in base units.</param>
/// <param name="ActiveSubstance">Active substance(s) (активно вещество), e.g. „Paracetamol 500 mg“.</param>
public sealed record ProductDetails(
    string Name,
    string? AtcCode,
    DispensingMode DispensingMode,
    decimal VatRate,
    bool IsActive,
    string? NhifCode = null,
    Guid? ManufacturerId = null,
    Guid? ProductGroupId = null,
    Guid? ShelfId = null,
    string? DefaultSaleUnitName = null,
    string? LatinName = null,
    string? GenericName = null,
    decimal? MarkupPercent = null,
    int? MinStock = null,
    int? MaxStock = null,
    string? ActiveSubstance = null);

/// <summary>A unit as requested by the user. <paramref name="Id"/> is null for a new unit.</summary>
public sealed record UnitSpec(Guid? Id, string Name, int Factor);

/// <summary>
/// A barcode as requested by the user. <paramref name="Id"/> is null for a new barcode.
/// The unit is identified by name because new units have no id yet (names are unique within a product).
/// </summary>
public sealed record BarcodeSpec(Guid? Id, string Code, string UnitName);

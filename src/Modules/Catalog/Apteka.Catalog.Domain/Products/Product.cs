using System.Text.RegularExpressions;
using Apteka.SharedKernel;

namespace Apteka.Catalog.Domain.Products;

/// <summary>
/// A medicine or other article. Owns its units and barcodes and guards their rules:
/// exactly one base unit (Factor = 1), unique unit names, valid and unique barcodes.
/// Nothing is removed: a product is deactivated, units and barcodes are corrected.
/// </summary>
public sealed partial class Product : Entity<Guid>
{
    private readonly List<ProductUnit> _units = [];
    private readonly List<Barcode> _barcodes = [];

    private Product()
    {
        Name = string.Empty;
    }

    private Product(Guid id)
        : base(id)
    {
        Name = string.Empty;
    }

    /// <summary>Номенклатурен номер: a short number given by the database, typed instead of the name.</summary>
    public long Code { get; private set; }

    public string Name { get; private set; }

    public string? LatinName { get; private set; }

    public string? GenericName { get; private set; }

    /// <summary>Trade markup of this product (търговска надценка); null means the group's.</summary>
    public decimal? MarkupPercent { get; private set; }

    /// <summary>Minimum stock in base units.</summary>
    public int? MinStock { get; private set; }

    /// <summary>Maximum stock in base units.</summary>
    public int? MaxStock { get; private set; }

    public string? AtcCode { get; private set; }

    public DispensingMode DispensingMode { get; private set; }

    /// <summary>Needs a prescription (ordinary or for narcotics).</summary>
    public bool IsRx => DispensingMode != DispensingMode.OverTheCounter;

    /// <summary>VAT in percent: 0, 9 or 20.</summary>
    public decimal VatRate { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Code in the NHIF (НЗОК) list; prices and reimbursement belong to the future Nhif module.</summary>
    public string? NhifCode { get; private set; }

    public Guid? ManufacturerId { get; private set; }

    public Guid? ProductGroupId { get; private set; }

    public Guid? ShelfId { get; private set; }

    /// <summary>The unit the till offers first; null means the largest unit.</summary>
    public Guid? DefaultSaleUnitId { get; private set; }

    public IReadOnlyList<ProductUnit> Units => _units;

    public IReadOnlyList<Barcode> Barcodes => _barcodes;

    public ProductUnit BaseUnit => _units.Single(u => u.IsBaseUnit);

    public static Result<Product> Create(ProductDetails details, IReadOnlyList<UnitSpec> units, IReadOnlyList<BarcodeSpec> barcodes)
    {
        var product = new Product(Guid.CreateVersion7());
        var result = product.Apply(details, units, barcodes);

        return result.IsSuccess ? Result.Success(product) : Result.Failure<Product>(result.Error);
    }

    /// <summary>Replaces the details, units and barcodes with the requested ones. Nothing changes if validation fails.</summary>
    public Result Update(ProductDetails details, IReadOnlyList<UnitSpec> units, IReadOnlyList<BarcodeSpec> barcodes) =>
        Apply(details, units, barcodes);

    private Result Apply(ProductDetails details, IReadOnlyList<UnitSpec> units, IReadOnlyList<BarcodeSpec> barcodes)
    {
        var normalized = Normalize(details);
        var requestedUnits = units.Select(u => u with { Name = u.Name.Trim() }).ToList();
        var requestedBarcodes = barcodes.Select(b => b with { Code = b.Code.Trim(), UnitName = b.UnitName.Trim() }).ToList();

        var error = ValidateDetails(normalized)
            ?? ValidateUnits(requestedUnits)
            ?? ValidateBarcodes(requestedBarcodes, requestedUnits)
            ?? ValidateDefaultSaleUnit(normalized.DefaultSaleUnitName, requestedUnits);
        if (error is not null)
        {
            return Result.Failure(error);
        }

        Name = normalized.Name;
        AtcCode = normalized.AtcCode;
        DispensingMode = normalized.DispensingMode;
        VatRate = normalized.VatRate;
        IsActive = normalized.IsActive;
        NhifCode = normalized.NhifCode;
        LatinName = normalized.LatinName;
        GenericName = normalized.GenericName;
        MarkupPercent = normalized.MarkupPercent;
        MinStock = normalized.MinStock;
        MaxStock = normalized.MaxStock;
        ManufacturerId = normalized.ManufacturerId;
        ProductGroupId = normalized.ProductGroupId;
        ShelfId = normalized.ShelfId;

        ApplyUnits(requestedUnits);
        ApplyBarcodes(requestedBarcodes);
        DefaultSaleUnitId = normalized.DefaultSaleUnitName is null ? null : UnitNamed(normalized.DefaultSaleUnitName).Id;

        return Result.Success();
    }

    private static ProductDetails Normalize(ProductDetails details) => details with
    {
        Name = details.Name.Trim(),
        AtcCode = string.IsNullOrWhiteSpace(details.AtcCode) ? null : details.AtcCode.Trim().ToUpperInvariant(),
        NhifCode = string.IsNullOrWhiteSpace(details.NhifCode) ? null : details.NhifCode.Trim().ToUpperInvariant(),
        DefaultSaleUnitName = string.IsNullOrWhiteSpace(details.DefaultSaleUnitName) ? null : details.DefaultSaleUnitName.Trim(),
        LatinName = string.IsNullOrWhiteSpace(details.LatinName) ? null : details.LatinName.Trim(),
        GenericName = string.IsNullOrWhiteSpace(details.GenericName) ? null : details.GenericName.Trim(),
    };

    private static Error? ValidateDetails(ProductDetails details)
    {
        if (details.Name.Length == 0)
        {
            return CatalogErrors.NameRequired;
        }

        if (details.Name.Length > CatalogErrors.MaxNameLength)
        {
            return CatalogErrors.NameTooLong;
        }

        if (details.LatinName is { Length: > CatalogErrors.MaxNameLength } || details.GenericName is { Length: > CatalogErrors.MaxNameLength })
        {
            return CatalogErrors.NameTooLong;
        }

        if (details.MarkupPercent is < 0 or > CatalogErrors.MaxMarkupPercent)
        {
            return CatalogErrors.InvalidMarkup;
        }

        if (details.MinStock < 0 || details.MaxStock < 0 || details.MinStock > details.MaxStock)
        {
            return CatalogErrors.InvalidStockLimits;
        }

        if (!Enum.IsDefined(details.DispensingMode))
        {
            return CatalogErrors.InvalidDispensingMode;
        }

        if (details.NhifCode is not null && !NhifCodePattern().IsMatch(details.NhifCode))
        {
            return CatalogErrors.InvalidNhifCode;
        }

        if (details.AtcCode is not null && !AtcCodePattern().IsMatch(details.AtcCode))
        {
            return CatalogErrors.InvalidAtcCode;
        }

        return VatRates.IsAllowed(details.VatRate) ? null : CatalogErrors.InvalidVatRate;
    }

    private Error? ValidateUnits(List<UnitSpec> units)
    {
        if (units.Count == 0)
        {
            return CatalogErrors.UnitsRequired;
        }

        foreach (var unit in units)
        {
            if (unit.Name.Length == 0)
            {
                return CatalogErrors.UnitNameRequired;
            }

            if (unit.Name.Length > CatalogErrors.MaxUnitNameLength)
            {
                return CatalogErrors.UnitNameTooLong;
            }

            if (unit.Factor < 1)
            {
                return CatalogErrors.FactorTooSmall;
            }

            if (unit.Id is { } id)
            {
                var existing = _units.Find(u => u.Id == id);
                if (existing is null)
                {
                    return CatalogErrors.UnknownUnit;
                }

                if (existing.Factor != unit.Factor)
                {
                    return CatalogErrors.UnitFactorChanged;
                }
            }
        }

        var duplicate = units.GroupBy(u => u.Name, StringComparer.CurrentCultureIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            return CatalogErrors.DuplicateUnitName(duplicate.Key);
        }

        if (units.Count(u => u.Factor == 1) != 1)
        {
            return CatalogErrors.ExactlyOneBaseUnit;
        }

        return _units.All(existing => units.Any(u => u.Id == existing.Id)) ? null : CatalogErrors.UnitRemoved;
    }

    private Error? ValidateBarcodes(List<BarcodeSpec> barcodes, List<UnitSpec> units)
    {
        foreach (var barcode in barcodes)
        {
            if (!Gtin.IsValid(barcode.Code))
            {
                return CatalogErrors.InvalidBarcode;
            }

            if (!units.Any(u => string.Equals(u.Name, barcode.UnitName, StringComparison.CurrentCultureIgnoreCase)))
            {
                return CatalogErrors.BarcodeUnitMissing;
            }

            if (barcode.Id is { } id && _barcodes.TrueForAll(b => b.Id != id))
            {
                return CatalogErrors.UnknownBarcode;
            }
        }

        var duplicate = barcodes.GroupBy(b => b.Code).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            return CatalogErrors.DuplicateBarcode(duplicate.Key);
        }

        return _barcodes.All(existing => barcodes.Any(b => b.Id == existing.Id)) ? null : CatalogErrors.BarcodeRemoved;
    }

    private static Error? ValidateDefaultSaleUnit(string? unitName, List<UnitSpec> units) =>
        unitName is null || units.Any(u => string.Equals(u.Name, unitName, StringComparison.CurrentCultureIgnoreCase))
            ? null
            : CatalogErrors.DefaultSaleUnitMissing;

    private ProductUnit UnitNamed(string name) =>
        _units.Single(u => string.Equals(u.Name, name, StringComparison.CurrentCultureIgnoreCase));

    private void ApplyUnits(List<UnitSpec> units)
    {
        foreach (var spec in units)
        {
            if (spec.Id is { } id)
            {
                var unit = _units.Single(u => u.Id == id);
                if (unit.Name != spec.Name)
                {
                    unit.Rename(spec.Name);
                }
            }
            else
            {
                _units.Add(new ProductUnit(Id, spec.Name, spec.Factor));
            }
        }
    }

    private void ApplyBarcodes(List<BarcodeSpec> barcodes)
    {
        foreach (var spec in barcodes)
        {
            var unitId = UnitNamed(spec.UnitName).Id;

            if (spec.Id is { } id)
            {
                var barcode = _barcodes.Single(b => b.Id == id);
                if (barcode.Code != spec.Code || barcode.ProductUnitId != unitId)
                {
                    barcode.Change(spec.Code, unitId);
                }
            }
            else
            {
                _barcodes.Add(new Barcode(Id, unitId, spec.Code));
            }
        }
    }

    // ATC: anatomical group letter, two digits, then optionally letter, letter, two digits (e.g. N, N02, N02B, N02BE, N02BE01).
    [GeneratedRegex("^[A-Z](\\d{2}([A-Z]([A-Z](\\d{2})?)?)?)?$", RegexOptions.CultureInvariant)]
    private static partial Regex AtcCodePattern();

    // NHIF list codes are short letter/digit codes (e.g. "PG123"); the exact format is checked against NHIF later.
    [GeneratedRegex("^[A-Z0-9]{1,20}$", RegexOptions.CultureInvariant)]
    private static partial Regex NhifCodePattern();
}

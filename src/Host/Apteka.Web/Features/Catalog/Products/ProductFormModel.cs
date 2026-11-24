using Apteka.Catalog.Application.Products;
using Apteka.Catalog.Domain.Products;

namespace Apteka.Web.Features.Catalog.Products;

/// <summary>Editable state of the product form; converted to <see cref="ProductInput"/> on save.</summary>
public sealed class ProductFormModel
{
    public string Name { get; set; } = string.Empty;

    public string? AtcCode { get; set; }

    public DispensingMode DispensingMode { get; set; } = DispensingMode.OverTheCounter;

    /// <summary>Whole percent (0, 9, 20): an int so that the select matches 20.00 from the database.</summary>
    public int VatRate { get; set; } = 20;

    public bool IsActive { get; set; } = true;

    public string? NhifCode { get; set; }

    /// <summary>Selected ids as text: "" means none (select elements bind strings reliably).</summary>
    public string ManufacturerId { get; set; } = string.Empty;

    public string ProductGroupId { get; set; } = string.Empty;

    public string ShelfId { get; set; } = string.Empty;

    /// <summary>"" means the largest unit.</summary>
    public string DefaultSaleUnitName { get; set; } = string.Empty;

    public List<UnitRow> Units { get; } = [];

    public List<BarcodeRow> Barcodes { get; } = [];

    public static ProductFormModel New()
    {
        var model = new ProductFormModel();
        model.Units.Add(new UnitRow { Name = "кутия", Factor = 1 });
        return model;
    }

    public static ProductFormModel From(ProductView view)
    {
        var model = new ProductFormModel
        {
            Name = view.Name,
            AtcCode = view.AtcCode,
            DispensingMode = view.DispensingMode,
            VatRate = (int)view.VatRate,
            IsActive = view.IsActive,
            NhifCode = view.NhifCode,
            ManufacturerId = view.ManufacturerId?.ToString() ?? string.Empty,
            ProductGroupId = view.ProductGroupId?.ToString() ?? string.Empty,
            ShelfId = view.ShelfId?.ToString() ?? string.Empty,
            DefaultSaleUnitName = view.DefaultSaleUnitName ?? string.Empty,
        };

        model.Units.AddRange(view.Units.Select(u => new UnitRow { Id = u.Id, Name = u.Name, Factor = u.Factor }));
        model.Barcodes.AddRange(view.Barcodes.Select(b => new BarcodeRow { Id = b.Id, Code = b.Code, UnitName = b.UnitName }));
        return model;
    }

    public ProductInput ToInput() => new(
        Name,
        AtcCode,
        DispensingMode,
        VatRate,
        IsActive,
        Units.Select(u => new UnitInput(u.Id, u.Name, u.Factor)).ToList(),
        Barcodes.Select(b => new BarcodeInput(b.Id, b.Code, b.UnitName)).ToList(),
        NhifCode,
        ParseId(ManufacturerId),
        ParseId(ProductGroupId),
        ParseId(ShelfId),
        string.IsNullOrEmpty(DefaultSaleUnitName) ? null : DefaultSaleUnitName);

    private static Guid? ParseId(string value) => Guid.TryParse(value, out var id) ? id : null;

    public sealed class UnitRow
    {
        public Guid? Id { get; init; }

        public string Name { get; set; } = string.Empty;

        public int Factor { get; set; } = 1;
    }

    public sealed class BarcodeRow
    {
        public Guid? Id { get; init; }

        public string Code { get; set; } = string.Empty;

        public string UnitName { get; set; } = string.Empty;
    }
}

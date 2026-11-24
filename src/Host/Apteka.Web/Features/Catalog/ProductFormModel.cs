using Apteka.Catalog.Application.Products;

namespace Apteka.Web.Features.Catalog;

/// <summary>Editable state of the product form; converted to <see cref="ProductInput"/> on save.</summary>
public sealed class ProductFormModel
{
    public string Name { get; set; } = string.Empty;

    public string? AtcCode { get; set; }

    public string? Manufacturer { get; set; }

    /// <summary>Whole percent (0, 9, 20): an int so that the select matches 20.00 from the database.</summary>
    public int VatRate { get; set; } = 20;

    public bool IsRx { get; set; }

    public bool IsActive { get; set; } = true;

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
            Manufacturer = view.Manufacturer,
            VatRate = (int)view.VatRate,
            IsRx = view.IsRx,
            IsActive = view.IsActive,
        };

        model.Units.AddRange(view.Units.Select(u => new UnitRow { Id = u.Id, Name = u.Name, Factor = u.Factor }));
        model.Barcodes.AddRange(view.Barcodes.Select(b => new BarcodeRow { Id = b.Id, Code = b.Code, UnitName = b.UnitName }));
        return model;
    }

    public ProductInput ToInput() => new(
        Name,
        AtcCode,
        Manufacturer,
        VatRate,
        IsRx,
        IsActive,
        Units.Select(u => new UnitInput(u.Id, u.Name, u.Factor)).ToList(),
        Barcodes.Select(b => new BarcodeInput(b.Id, b.Code, b.UnitName)).ToList());

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

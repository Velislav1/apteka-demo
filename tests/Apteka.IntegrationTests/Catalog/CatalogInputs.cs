using Apteka.Catalog.Application.Products;
using Apteka.Catalog.Domain.Products;

namespace Apteka.IntegrationTests.Catalog;

internal static class CatalogInputs
{
    public static ProductInput Tablets(string name, string boxBarcode) => new(
        name,
        "N02BE01",
        DispensingMode.OverTheCounter,
        20m,
        IsActive: true,
        [new UnitInput(null, "кутия", 20), new UnitInput(null, "блистер", 10), new UnitInput(null, "таблетка", 1)],
        [new BarcodeInput(null, boxBarcode, "кутия")]);

    /// <summary>The input that saves a stored product unchanged; tests change it with <c>with</c>.</summary>
    public static ProductInput FromView(ProductView view) => new(
        view.Name,
        view.AtcCode,
        view.DispensingMode,
        view.VatRate,
        view.IsActive,
        [.. view.Units.Select(u => new UnitInput(u.Id, u.Name, u.Factor))],
        [.. view.Barcodes.Select(b => new BarcodeInput(b.Id, b.Code, b.UnitName))],
        view.NhifCode,
        view.ManufacturerId,
        view.ProductGroupId,
        view.ShelfId,
        view.DefaultSaleUnitName);

    public static Product ToProduct(ProductInput input) => Product.Create(
        new ProductDetails(input.Name, input.AtcCode, input.DispensingMode, input.VatRate, input.IsActive),
        [.. input.Units.Select(u => new UnitSpec(u.Id, u.Name, u.Factor))],
        [.. input.Barcodes.Select(b => new BarcodeSpec(b.Id, b.Code, b.UnitName))]).Value;

    public static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}";
}

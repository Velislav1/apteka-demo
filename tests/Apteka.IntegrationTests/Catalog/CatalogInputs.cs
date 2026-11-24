using Apteka.Catalog.Application.Products;

namespace Apteka.IntegrationTests.Catalog;

internal static class CatalogInputs
{
    public static ProductInput Tablets(string name, string boxBarcode) => new(
        name,
        "N02BE01",
        "Демо Фарма АД",
        20m,
        IsRx: false,
        IsActive: true,
        [new UnitInput(null, "кутия", 20), new UnitInput(null, "блистер", 10), new UnitInput(null, "таблетка", 1)],
        [new BarcodeInput(null, boxBarcode, "кутия")]);

    public static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}";
}

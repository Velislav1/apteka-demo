using Apteka.Catalog.Domain.Products;

namespace Apteka.Catalog.Tests.Products;

public class ProductTests
{
    private const string ValidEan = "2000000000015"; // check digit 5

    private static readonly ProductDetails Details = new("Парацетамол 500 мг", "N02BE01", DispensingMode.OverTheCounter, 20m, IsActive: true);

    private static List<UnitSpec> BoxBlisterTablet() =>
    [
        new(null, "кутия", 30),
        new(null, "блистер", 10),
        new(null, "таблетка", 1),
    ];

    private static Product CreateValid(params BarcodeSpec[] barcodes)
    {
        var result = Product.Create(Details, BoxBlisterTablet(), barcodes);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        return result.Value;
    }

    [Fact]
    public void Valid_product_has_one_base_unit_and_converts_units_by_factor()
    {
        var product = CreateValid(new BarcodeSpec(null, ValidEan, "кутия"));

        Assert.Equal("таблетка", product.BaseUnit.Name);
        Assert.Equal(60, product.Units.Single(u => u.Name == "кутия").ToBaseUnits(2).Value);
        Assert.Equal(10, product.Units.Single(u => u.Name == "блистер").ToBaseUnits(1).Value);

        var barcode = Assert.Single(product.Barcodes);
        Assert.Equal(product.Units.Single(u => u.Name == "кутия").Id, barcode.ProductUnitId);
        Assert.All(product.Units, u => Assert.Equal(product.Id, u.ProductId));
    }

    [Fact]
    public void Input_is_trimmed_and_ATC_code_is_upper_cased()
    {
        var result = Product.Create(
            Details with { Name = "  Аспирин  ", AtcCode = " b01ac06 ", NhifCode = " pg123 " },
            [new UnitSpec(null, " брой ", 1)],
            []);

        Assert.True(result.IsSuccess);
        Assert.Equal("Аспирин", result.Value.Name);
        Assert.Equal("B01AC06", result.Value.AtcCode);
        Assert.Equal("PG123", result.Value.NhifCode);
        Assert.Equal("брой", result.Value.BaseUnit.Name);
    }

    [Fact]
    public void Product_without_a_base_unit_is_rejected()
    {
        var result = Product.Create(Details, [new UnitSpec(null, "кутия", 30), new UnitSpec(null, "блистер", 10)], []);

        Assert.Equal(CatalogErrors.ExactlyOneBaseUnit, result.Error);
    }

    [Fact]
    public void Product_with_two_base_units_is_rejected()
    {
        var result = Product.Create(Details, [new UnitSpec(null, "таблетка", 1), new UnitSpec(null, "брой", 1)], []);

        Assert.Equal(CatalogErrors.ExactlyOneBaseUnit, result.Error);
    }

    [Fact]
    public void Product_without_units_is_rejected()
    {
        Assert.Equal(CatalogErrors.UnitsRequired, Product.Create(Details, [], []).Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Factor_below_one_is_rejected(int factor)
    {
        var result = Product.Create(Details, [new UnitSpec(null, "таблетка", 1), new UnitSpec(null, "кутия", factor)], []);

        Assert.Equal(CatalogErrors.FactorTooSmall, result.Error);
    }

    [Fact]
    public void Unit_names_must_be_unique_ignoring_case()
    {
        var result = Product.Create(Details, [new UnitSpec(null, "Таблетка", 1), new UnitSpec(null, "таблетка", 10)], []);

        Assert.Equal("catalog.duplicate_unit", result.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Name_is_required(string name)
    {
        Assert.Equal(CatalogErrors.NameRequired, Product.Create(Details with { Name = name }, BoxBlisterTablet(), []).Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(20)]
    public void Bulgarian_VAT_rates_are_allowed(decimal rate)
    {
        Assert.True(Product.Create(Details with { VatRate = rate }, BoxBlisterTablet(), []).IsSuccess);
    }

    [Theory]
    [InlineData(18)]
    [InlineData(-1)]
    [InlineData(20.5)]
    public void Other_VAT_rates_are_rejected(decimal rate)
    {
        Assert.Equal(CatalogErrors.InvalidVatRate, Product.Create(Details with { VatRate = rate }, BoxBlisterTablet(), []).Error);
    }

    [Theory]
    [InlineData("N02BE1")]
    [InlineData("02BE01")]
    [InlineData("N02BE011")]
    public void Malformed_ATC_code_is_rejected(string atc)
    {
        Assert.Equal(CatalogErrors.InvalidAtcCode, Product.Create(Details with { AtcCode = atc }, BoxBlisterTablet(), []).Error);
    }

    [Fact]
    public void Barcode_with_wrong_check_digit_is_rejected()
    {
        var result = Product.Create(Details, BoxBlisterTablet(), [new BarcodeSpec(null, "2000000000016", "кутия")]);

        Assert.Equal(CatalogErrors.InvalidBarcode, result.Error);
    }

    [Fact]
    public void Same_barcode_twice_in_one_product_is_rejected()
    {
        var result = Product.Create(
            Details,
            BoxBlisterTablet(),
            [new BarcodeSpec(null, ValidEan, "кутия"), new BarcodeSpec(null, ValidEan, "блистер")]);

        Assert.Equal("catalog.duplicate_barcode", result.Error.Code);
    }

    [Fact]
    public void Barcode_must_point_to_a_unit_of_the_product()
    {
        var result = Product.Create(Details, BoxBlisterTablet(), [new BarcodeSpec(null, ValidEan, "флакон")]);

        Assert.Equal(CatalogErrors.BarcodeUnitMissing, result.Error);
    }

    [Fact]
    public void Update_can_rename_units_add_units_and_change_barcodes()
    {
        var product = CreateValid(new BarcodeSpec(null, ValidEan, "кутия"));
        var box = product.Units.Single(u => u.Name == "кутия");
        var tablet = product.Units.Single(u => u.Name == "таблетка");
        var blister = product.Units.Single(u => u.Name == "блистер");
        var barcode = product.Barcodes.Single();

        var result = product.Update(
            Details with { Name = "Парацетамол 500 мг табл.", IsActive = false },
            [
                new UnitSpec(box.Id, "опаковка", 30),
                new UnitSpec(blister.Id, "блистер", 10),
                new UnitSpec(tablet.Id, "таблетка", 1),
                new UnitSpec(null, "стек", 300),
            ],
            [new BarcodeSpec(barcode.Id, "2000000000022", "опаковка"), new BarcodeSpec(null, "2000000000039", "стек")]);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.False(product.IsActive);
        Assert.Equal("опаковка", box.Name);
        Assert.Equal(4, product.Units.Count);
        Assert.Equal("2000000000022", barcode.Code);
        Assert.Equal(2, product.Barcodes.Count);
    }

    [Fact]
    public void Factor_of_an_existing_unit_cannot_change()
    {
        var product = CreateValid();
        var units = product.Units.Select(u => new UnitSpec(u.Id, u.Name, u.Name == "кутия" ? 20 : u.Factor)).ToList();

        Assert.Equal(CatalogErrors.UnitFactorChanged, product.Update(Details, units, []).Error);
    }

    [Fact]
    public void Existing_unit_cannot_be_removed()
    {
        var product = CreateValid();
        var units = product.Units.Where(u => u.Name != "блистер").Select(u => new UnitSpec(u.Id, u.Name, u.Factor)).ToList();

        Assert.Equal(CatalogErrors.UnitRemoved, product.Update(Details, units, []).Error);
    }

    [Fact]
    public void Existing_barcode_cannot_be_removed()
    {
        var product = CreateValid(new BarcodeSpec(null, ValidEan, "кутия"));
        var units = product.Units.Select(u => new UnitSpec(u.Id, u.Name, u.Factor)).ToList();

        Assert.Equal(CatalogErrors.BarcodeRemoved, product.Update(Details, units, []).Error);
    }

    [Fact]
    public void Failed_update_changes_nothing()
    {
        var product = CreateValid(new BarcodeSpec(null, ValidEan, "кутия"));
        var units = product.Units.Select(u => new UnitSpec(u.Id, u.Name + " нов", u.Factor)).ToList();

        var result = product.Update(Details with { Name = "Друго име", VatRate = 18m }, units, []);

        Assert.True(result.IsFailure);
        Assert.Equal("Парацетамол 500 мг", product.Name);
        Assert.Contains(product.Units, u => u.Name == "кутия");
        Assert.Single(product.Barcodes);
    }

    [Theory]
    [InlineData(DispensingMode.OverTheCounter, false)]
    [InlineData(DispensingMode.Prescription, true)]
    [InlineData(DispensingMode.Narcotic, true)]
    public void Prescription_and_narcotic_products_need_a_prescription(DispensingMode mode, bool isRx)
    {
        var product = Product.Create(Details with { DispensingMode = mode }, BoxBlisterTablet(), []).Value;

        Assert.Equal(mode, product.DispensingMode);
        Assert.Equal(isRx, product.IsRx);
    }

    [Fact]
    public void Unknown_dispensing_mode_is_rejected()
    {
        var result = Product.Create(Details with { DispensingMode = (DispensingMode)9 }, BoxBlisterTablet(), []);

        Assert.Equal(CatalogErrors.InvalidDispensingMode, result.Error);
    }

    [Theory]
    [InlineData("PG-123")]
    [InlineData("ПГ123")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")]
    public void Malformed_NHIF_code_is_rejected(string code)
    {
        Assert.Equal(CatalogErrors.InvalidNhifCode, Product.Create(Details with { NhifCode = code }, BoxBlisterTablet(), []).Error);
    }

    [Fact]
    public void Default_sale_unit_points_to_one_of_the_units()
    {
        var product = Product.Create(Details with { DefaultSaleUnitName = "Блистер" }, BoxBlisterTablet(), []).Value;

        Assert.Equal(product.Units.Single(u => u.Name == "блистер").Id, product.DefaultSaleUnitId);
    }

    [Fact]
    public void Default_sale_unit_that_is_not_a_unit_is_rejected()
    {
        var result = Product.Create(Details with { DefaultSaleUnitName = "флакон" }, BoxBlisterTablet(), []);

        Assert.Equal(CatalogErrors.DefaultSaleUnitMissing, result.Error);
    }

    [Fact]
    public void Default_sale_unit_can_be_a_unit_added_in_the_same_update()
    {
        var product = CreateValid();
        var units = product.Units.Select(u => new UnitSpec(u.Id, u.Name, u.Factor)).Append(new UnitSpec(null, "стек", 300)).ToList();

        Assert.True(product.Update(Details with { DefaultSaleUnitName = "стек" }, units, []).IsSuccess);
        Assert.Equal(product.Units.Single(u => u.Name == "стек").Id, product.DefaultSaleUnitId);
    }

    [Fact]
    public void Product_keeps_the_chosen_manufacturer_group_and_shelf()
    {
        var (manufacturer, group, shelf) = (Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());

        var product = Product.Create(
            Details with { ManufacturerId = manufacturer, ProductGroupId = group, ShelfId = shelf },
            BoxBlisterTablet(),
            []).Value;

        Assert.Equal((manufacturer, group, shelf), (product.ManufacturerId!.Value, product.ProductGroupId!.Value, product.ShelfId!.Value));
    }

    [Fact]
    public void Latin_and_generic_names_markup_and_stock_limits_are_kept()
    {
        var product = Product.Create(
            Details with { LatinName = " PARACETAMOL TABL ", GenericName = "Paracetamol", MarkupPercent = 30m, MinStock = 20, MaxStock = 100 },
            BoxBlisterTablet(),
            []).Value;

        Assert.Equal("PARACETAMOL TABL", product.LatinName);
        Assert.Equal("Paracetamol", product.GenericName);
        Assert.Equal(30m, product.MarkupPercent);
        Assert.Equal((20, 100), (product.MinStock!.Value, product.MaxStock!.Value));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Markup_outside_0_to_1000_percent_is_rejected(decimal markup) =>
        Assert.Equal(CatalogErrors.InvalidMarkup, Product.Create(Details with { MarkupPercent = markup }, BoxBlisterTablet(), []).Error);

    [Theory]
    [InlineData(-1, null)]
    [InlineData(50, 10)]
    public void Invalid_stock_limits_are_rejected(int? min, int? max) =>
        Assert.Equal(CatalogErrors.InvalidStockLimits, Product.Create(Details with { MinStock = min, MaxStock = max }, BoxBlisterTablet(), []).Error);
}

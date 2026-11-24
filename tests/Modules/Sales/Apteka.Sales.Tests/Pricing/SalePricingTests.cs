using Apteka.Sales.Domain.Pricing;

namespace Apteka.Sales.Tests.Pricing;

public class SalePricingTests
{
    [Fact]
    public void Unit_price_is_the_entered_price_when_selling_the_same_unit()
    {
        // A box of 30 priced at 10,00 €: base price 0,3333 € would give 9,999 € → but the box is still 10,00 €.
        Assert.Equal(10.00m, SalePricing.UnitPrice(soldUnitFactor: 30, pricedUnitFactor: 30, pricedUnitSalePrice: 10m, salePricePerBaseUnit: 0.3333m));
    }

    [Fact]
    public void Unit_price_of_another_unit_comes_from_the_base_price_rounded_to_cents()
    {
        Assert.Equal(3.33m, SalePricing.UnitPrice(soldUnitFactor: 10, pricedUnitFactor: 30, pricedUnitSalePrice: 10m, salePricePerBaseUnit: 0.3333m));
        Assert.Equal(0.33m, SalePricing.UnitPrice(soldUnitFactor: 1, pricedUnitFactor: 30, pricedUnitSalePrice: 10m, salePricePerBaseUnit: 0.3333m));
    }

    [Fact]
    public void Line_total_is_price_times_quantity_minus_discount_with_VAT_inside()
    {
        var line = SalePricing.Line(unitPrice: 6.49m, quantity: 3, discountPercent: 10m, vatRate: 20m);

        Assert.Equal(19.47m, line.Gross);
        Assert.Equal(1.95m, line.Discount); // 1,947 → 1,95
        Assert.Equal(17.52m, line.Total);
        Assert.Equal(2.92m, line.Vat); // 17,52 × 20/120 = 2,92
    }

    [Theory]
    [InlineData(0.005, 0.01)]
    [InlineData(0.004, 0.00)]
    [InlineData(2.675, 2.68)]
    public void Amounts_are_rounded_half_away_from_zero(decimal amount, decimal expected) =>
        Assert.Equal(expected, SalePricing.Round(amount));

    [Fact]
    public void Zero_VAT_line_has_no_VAT() =>
        Assert.Equal(0m, SalePricing.Line(5m, 2, 0m, 0m).Vat);
}

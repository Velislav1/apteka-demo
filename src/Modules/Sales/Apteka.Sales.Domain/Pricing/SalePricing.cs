namespace Apteka.Sales.Domain.Pricing;

/// <summary>
/// Retail prices include VAT. A line: unit price × quantity − discount, rounded to cents;
/// the VAT inside it is computed from the rounded total (only needed for reports and the receipt).
/// </summary>
public static class SalePricing
{
    /// <summary>
    /// Price of one sold unit from a batch: exactly the price that was entered if it is the same unit,
    /// otherwise the base-unit price × factor, rounded to cents.
    /// </summary>
    public static decimal UnitPrice(int soldUnitFactor, int pricedUnitFactor, decimal pricedUnitSalePrice, decimal salePricePerBaseUnit) =>
        soldUnitFactor == pricedUnitFactor
            ? Round(pricedUnitSalePrice)
            : Round(salePricePerBaseUnit * soldUnitFactor);

    public static LineAmounts Line(decimal unitPrice, int quantity, decimal discountPercent, decimal vatRate)
    {
        var gross = Round(unitPrice * quantity);
        var discount = Round(gross * discountPercent / 100m);
        var total = gross - discount;
        var vat = Round(total * vatRate / (100m + vatRate));
        return new LineAmounts(gross, discount, total, vat);
    }

    public static decimal Round(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}

/// <param name="Gross">Before discount.</param>
/// <param name="Total">What the customer pays for the line (VAT included).</param>
/// <param name="Vat">VAT contained in <paramref name="Total"/>.</param>
public sealed record LineAmounts(decimal Gross, decimal Discount, decimal Total, decimal Vat);

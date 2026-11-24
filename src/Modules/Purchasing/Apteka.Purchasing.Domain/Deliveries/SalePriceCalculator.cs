namespace Apteka.Purchasing.Domain.Deliveries;

/// <summary>
/// Suggested sale price from a markup, as in Apteka Expert 2: purchase price (without VAT) + markup %, then + VAT,
/// rounded to cents. The user can always type a different sale price.
/// </summary>
public static class SalePriceCalculator
{
    public static decimal FromMarkup(decimal purchasePriceWithoutVat, decimal markupPercent, decimal vatPercent) =>
        decimal.Round(
            purchasePriceWithoutVat * (1 + (markupPercent / 100m)) * (1 + (vatPercent / 100m)),
            2,
            MidpointRounding.AwayFromZero);
}

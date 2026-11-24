namespace Apteka.Catalog.Domain.Products;

/// <summary>VAT rates allowed in Bulgaria, in percent.</summary>
public static class VatRates
{
    public const decimal Zero = 0m;
    public const decimal Reduced = 9m;
    public const decimal Standard = 20m;

    public static IReadOnlyList<decimal> All { get; } = [Zero, Reduced, Standard];

    public static bool IsAllowed(decimal rate) => All.Contains(rate);
}

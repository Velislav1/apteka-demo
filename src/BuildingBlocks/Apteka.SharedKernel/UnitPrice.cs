using System.Globalization;

namespace Apteka.SharedKernel;

/// <summary>
/// A price for one unit (a box, a tablet), in euro with four decimal places, rounded away from zero.
/// Line totals and amounts to pay are <see cref="Money"/> (two decimal places).
/// </summary>
public readonly record struct UnitPrice : IComparable<UnitPrice>
{
    public const int Decimals = 4;

    private static readonly CultureInfo BulgarianCulture = CultureInfo.GetCultureInfo("bg-BG");

    private UnitPrice(decimal amount)
    {
        Amount = amount;
    }

    public decimal Amount { get; }

    public static UnitPrice Zero => new(0m);

    public static UnitPrice Of(decimal amount) =>
        amount < 0
            ? throw new ArgumentOutOfRangeException(nameof(amount), amount, "A price cannot be negative.")
            : new UnitPrice(decimal.Round(amount, Decimals, MidpointRounding.AwayFromZero));

    /// <summary>The price of one base unit when <paramref name="factor"/> base units cost this price.</summary>
    public UnitPrice PerBaseUnit(int factor) =>
        factor < 1
            ? throw new ArgumentOutOfRangeException(nameof(factor), factor, "Factor must be at least 1.")
            : Of(Amount / factor);

    /// <summary>The amount for <paramref name="count"/> units, rounded to cents.</summary>
    public Money Times(int count) => Money.Of(Amount * count);

    public int CompareTo(UnitPrice other) => Amount.CompareTo(other.Amount);

    public override string ToString() => Amount.ToString("#,##0.00##", BulgarianCulture) + " €";
}

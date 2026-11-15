using System.Globalization;

namespace Apteka.SharedKernel;

/// <summary>An amount in euro, always rounded to two decimal places (away from zero).</summary>
public readonly record struct Money : IComparable<Money>
{
    public const string Currency = "EUR";

    private static readonly CultureInfo BulgarianCulture = CultureInfo.GetCultureInfo("bg-BG");

    private Money(decimal amount)
    {
        Amount = amount;
    }

    public decimal Amount { get; }

    public static Money Zero => new(0m);

    public static Money Of(decimal amount) =>
        new(decimal.Round(amount, 2, MidpointRounding.AwayFromZero));

    public static Money operator +(Money left, Money right) => Of(left.Amount + right.Amount);

    public static Money operator -(Money left, Money right) => Of(left.Amount - right.Amount);

    public static Money operator -(Money value) => Of(-value.Amount);

    public static Money operator *(Money value, decimal factor) => Of(value.Amount * factor);

    public static Money operator *(Money value, Quantity quantity) => Of(value.Amount * quantity.Value);

    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;

    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;

    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;

    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;

    public int CompareTo(Money other) => Amount.CompareTo(other.Amount);

    public override string ToString() => Amount.ToString("N2", BulgarianCulture) + " €";
}
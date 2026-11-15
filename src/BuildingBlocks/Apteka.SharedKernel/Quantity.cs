namespace Apteka.SharedKernel;

/// <summary>A non-negative whole number of the smallest unit (BaseUnit) of a product.</summary>
public readonly record struct Quantity : IComparable<Quantity>
{
    private Quantity(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public static Quantity Zero => new(0);

    public static Quantity Of(int value) =>
        value < 0
            ? throw new ArgumentOutOfRangeException(nameof(value), value, "Quantity cannot be negative.")
            : new Quantity(value);

    /// <summary>Converts a count of a larger unit (box, blister) to base units using its Factor.</summary>
    public static Quantity FromUnits(int count, int factor)
    {
        if (factor < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(factor), factor, "Factor must be at least 1.");
        }

        return Of(checked(count * factor));
    }

    public static Quantity operator +(Quantity left, Quantity right) => Of(checked(left.Value + right.Value));

    public static Quantity operator -(Quantity left, Quantity right) =>
        left.Value < right.Value
            ? throw new InvalidOperationException("Quantity cannot become negative.")
            : new Quantity(left.Value - right.Value);

    public static bool operator <(Quantity left, Quantity right) => left.Value < right.Value;

    public static bool operator >(Quantity left, Quantity right) => left.Value > right.Value;

    public static bool operator <=(Quantity left, Quantity right) => left.Value <= right.Value;

    public static bool operator >=(Quantity left, Quantity right) => left.Value >= right.Value;

    public int CompareTo(Quantity other) => Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
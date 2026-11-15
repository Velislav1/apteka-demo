using Apteka.SharedKernel;

namespace Apteka.SharedKernel.Tests;

public class QuantityTests
{
    [Fact]
    public void Of_rejects_negative_values()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Quantity.Of(-1));
    }

    [Fact]
    public void FromUnits_converts_with_factor()
    {
        Assert.Equal(Quantity.Of(60), Quantity.FromUnits(2, 30));
    }

    [Fact]
    public void FromUnits_rejects_factor_below_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Quantity.FromUnits(1, 0));
    }

    [Fact]
    public void Subtraction_below_zero_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => Quantity.Of(1) - Quantity.Of(2));
        Assert.Equal(Quantity.Of(1), Quantity.Of(3) - Quantity.Of(2));
    }
}
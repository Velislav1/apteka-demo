using Apteka.SharedKernel;

namespace Apteka.SharedKernel.Tests;

public class MoneyTests
{
    [Theory]
    [InlineData(1.005, 1.01)]
    [InlineData(1.004, 1.00)]
    [InlineData(-1.005, -1.01)]
    [InlineData(2.5, 2.50)]
    public void Of_rounds_to_two_decimals_away_from_zero(double input, double expected)
    {
        Assert.Equal((decimal)expected, Money.Of((decimal)input).Amount);
    }

    [Fact]
    public void Arithmetic_keeps_two_decimals()
    {
        var total = Money.Of(0.10m) + Money.Of(0.20m);

        Assert.Equal(Money.Of(0.30m), total);
        Assert.Equal(Money.Of(7.50m), Money.Of(2.50m) * Quantity.Of(3));
        Assert.Equal(Money.Of(1.07m), Money.Of(0.99m) * 1.08m);
    }

    [Fact]
    public void Default_is_zero_and_comparison_works()
    {
        Assert.Equal(Money.Zero, default);
        Assert.True(Money.Of(2) > Money.Of(1.99m));
    }

    [Fact]
    public void ToString_uses_bulgarian_format_with_euro()
    {
        Assert.EndsWith(" €", Money.Of(1234.5m).ToString());
        Assert.Contains(",50", Money.Of(1234.5m).ToString());
    }
}
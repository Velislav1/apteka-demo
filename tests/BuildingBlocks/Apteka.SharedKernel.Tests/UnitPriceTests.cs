namespace Apteka.SharedKernel.Tests;

public class UnitPriceTests
{
    [Fact]
    public void Price_is_rounded_to_four_decimals_away_from_zero()
    {
        Assert.Equal(1.2346m, UnitPrice.Of(1.23455m).Amount);
        Assert.Equal(0.3333m, UnitPrice.Of(10m / 30).Amount);
    }

    [Fact]
    public void Negative_price_is_rejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => UnitPrice.Of(-0.01m));

    [Fact]
    public void Box_price_is_split_into_base_unit_price()
    {
        Assert.Equal(0.3333m, UnitPrice.Of(10m).PerBaseUnit(30).Amount);
        Assert.Equal(10m, UnitPrice.Of(10m).PerBaseUnit(1).Amount);
        Assert.Throws<ArgumentOutOfRangeException>(() => UnitPrice.Of(10m).PerBaseUnit(0));
    }

    [Fact]
    public void Total_for_a_count_is_rounded_to_cents() =>
        Assert.Equal(Money.Of(4.94m), UnitPrice.Of(1.2345m).Times(4));

    [Fact]
    public void Shown_in_Bulgarian_format() =>
        Assert.Equal("1 234,50 €", UnitPrice.Of(1234.5m).ToString().Replace(' ', ' '));
}

public class BulgarianTimeTests
{
    [Fact]
    public void Sofia_date_differs_from_UTC_date_late_in_the_evening()
    {
        // 22:30 UTC on 30 June is 01:30 on 1 July in Sofia (UTC+3 in summer).
        Assert.Equal(new DateOnly(2026, 7, 1), BulgarianTime.Today(new DateTimeOffset(2026, 6, 30, 22, 30, 0, TimeSpan.Zero)));
        // 22:30 UTC on 30 December is 00:30 on 31 December (UTC+2 in winter).
        Assert.Equal(new DateOnly(2026, 12, 31), BulgarianTime.Today(new DateTimeOffset(2026, 12, 30, 22, 30, 0, TimeSpan.Zero)));
    }
}

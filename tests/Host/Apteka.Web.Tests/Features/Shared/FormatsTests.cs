using Apteka.Catalog.Contracts;
using Apteka.Web.Features.Shared;

namespace Apteka.Web.Tests.Features.Shared;

public class FormatsTests
{
    private static readonly CatalogUnit[] Units =
    [
        new(Guid.CreateVersion7(), "таблетка", 1),
        new(Guid.CreateVersion7(), "блистер", 10),
        new(Guid.CreateVersion7(), "кутия", 30),
    ];

    [Theory]
    [InlineData(65, "2 × кутия + 5 × таблетка")]
    [InlineData(75, "2 × кутия + 1 × блистер + 5 × таблетка")]
    [InlineData(30, "1 × кутия")]
    [InlineData(7, "7 × таблетка")]
    [InlineData(0, "0")]
    public void Quantity_is_split_into_whole_largest_units(int baseQuantity, string expected) =>
        Assert.Equal(expected, Formats.Quantity(baseQuantity, Units));

    [Fact]
    public void Euro_and_dates_use_Bulgarian_format()
    {
        Assert.Equal("1 234,50 €", Formats.Euro(1234.5m).Replace(' ', ' '));
        Assert.Equal("05.03.2027", Formats.Date(new DateOnly(2027, 3, 5)));
        Assert.Equal("01.07.2026 01:30", Formats.LocalTime(new DateTimeOffset(2026, 6, 30, 22, 30, 0, TimeSpan.Zero)));
    }
}

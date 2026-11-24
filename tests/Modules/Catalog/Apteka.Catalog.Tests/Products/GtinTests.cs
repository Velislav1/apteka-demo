using Apteka.Catalog.Domain.Products;

namespace Apteka.Catalog.Tests.Products;

public class GtinTests
{
    [Theory]
    [InlineData("4006381333931")] // EAN-13
    [InlineData("96385074")] // EAN-8
    [InlineData("036000291452")] // UPC-A
    [InlineData("2000000000015")]
    public void Valid_codes_pass(string code) => Assert.True(Gtin.IsValid(code));

    [Theory]
    [InlineData("4006381333932")] // wrong check digit
    [InlineData("400638133393")] // 12 digits but not a valid UPC-A
    [InlineData("40063813339")] // 11 digits
    [InlineData("400638133393A")]
    [InlineData("")]
    public void Invalid_codes_fail(string code) => Assert.False(Gtin.IsValid(code));

    [Fact]
    public void Check_digit_is_computed_with_weights_3_and_1_from_the_right()
    {
        Assert.Equal(1, Gtin.CheckDigit("400638133393"));
        Assert.Equal(5, Gtin.CheckDigit("200000000001"));
    }
}

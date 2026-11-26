using Apteka.Web.Features.Shared;

namespace Apteka.Web.Tests.Features.Shared;

public class Ean13SvgTests
{
    [Fact]
    public void Encodes_the_standard_example_and_draws_only_valid_codes()
    {
        // 4006381333931: the textbook EAN-13 (first digit 4 → left parity LGLLGG).
        var bits = Ean13Svg.Modules("4006381333931")!;

        Assert.Equal(95, bits.Length);
        Assert.StartsWith("101" + "0001101" + "0100111", bits, StringComparison.Ordinal); // guard, 0 (L), 0 (G)
        Assert.EndsWith("1100110" + "101", bits, StringComparison.Ordinal); // 1 (R), guard
        Assert.Contains("aria-label=\"4006381333931\"", Ean13Svg.Render("4006381333931"), StringComparison.Ordinal);
        Assert.Null(Ean13Svg.Modules("12345"));
        Assert.Equal(string.Empty, Ean13Svg.Render("abc"));
    }
}

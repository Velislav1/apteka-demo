using Apteka.Catalog.Contracts;

namespace Apteka.Catalog.Tests.Codes;

public class Gs1CodeTests
{
    private const char Gs = '\u001d';

    [Fact]
    public void Reads_gtin_expiry_batch_and_serial_in_any_order_with_the_group_separator()
    {
        var code = Gs1Code.Parse($"]d2010200000000001521ABC123XYZ{Gs}17271231{Gs}10L-2026A");

        Assert.Equal(new Gs1Code("02000000000015", "L-2026A", new DateOnly(2027, 12, 31), "ABC123XYZ"), code);
        Assert.Equal("2000000000015", code!.Ean13);
    }

    [Theory]
    [InlineData("(01)02000000000015(17)280600(10)B77(21)S1")]
    [InlineData("010200000000001517280600|10B77|21S1")]
    [InlineData("01020000000000151728060010B77~21S1")]
    public void Reads_the_readable_form_and_separator_stand_ins_and_day_00_is_the_end_of_the_month(string text)
    {
        var code = Gs1Code.Parse(text)!;

        Assert.Equal(("B77", new DateOnly(2028, 6, 30), "S1"), (code.BatchNo, code.ExpiryDate, code.SerialNumber));
    }

    [Theory]
    [InlineData("2000000000015", false)]
    [InlineData("Парацетамол", false)]
    [InlineData("0102000000000015", false)]
    [InlineData("010200000000001510A", true)]
    [InlineData("(01)02000000000015", true)]
    public void Only_gs1_element_strings_are_taken_for_gs1(string text, bool expected) => Assert.Equal(expected, Gs1Code.LooksLikeGs1(text));

    [Fact]
    public void A_wrong_gtin_check_digit_or_a_bad_month_gives_nothing_or_no_date()
    {
        Assert.Null(Gs1Code.Parse("010200000000001610A"));
        Assert.Null(Gs1Code.Parse("01020000000000151727133110A")!.ExpiryDate);
    }
}

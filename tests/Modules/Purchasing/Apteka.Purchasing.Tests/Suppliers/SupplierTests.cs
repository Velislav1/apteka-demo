using Apteka.Purchasing.Domain.Suppliers;
using Apteka.Purchasing.Infrastructure.Seed;

namespace Apteka.Purchasing.Tests.Suppliers;

public class EikTests
{
    [Theory]
    [InlineData("200000015")] // made-up demo EIK: 8 digits + check digit
    [InlineData("000000000")]
    public void Valid_nine_digit_EIK_passes(string eik) => Assert.True(Eik.IsValid(eik));

    [Theory]
    [InlineData("200000016")] // wrong check digit
    [InlineData("20000001")] // 8 digits
    [InlineData("20000001A")]
    [InlineData("")]
    public void Invalid_EIK_fails(string eik) => Assert.False(Eik.IsValid(eik));

    [Fact]
    public void Check_digit_falls_back_to_the_second_weights_and_then_to_zero()
    {
        int[] first = [1, 2, 3, 4, 5, 6, 7, 8];
        int[] second = [3, 4, 5, 6, 7, 8, 9, 10];

        // 1*8 = 8 with the first weights.
        Assert.Equal(8, Eik.CheckDigit("00000001", first, second));

        // 2*1 + 1*8 = 10 → second weights: 2*3 + 1*10 = 16 % 11 = 5.
        Assert.Equal(5, Eik.CheckDigit("20000001", first, second));

        // 5*2 = 10 → second weights: 5*4 = 20 % 11 = 9.
        Assert.Equal(9, Eik.CheckDigit("05000000", first, second));

        // 1*1 + 3*3 = 10 → second weights: 1*3 + 3*5 = 18 % 11 = 7.
        Assert.Equal(7, Eik.CheckDigit("10300000", first, second));
    }

    [Fact]
    public void Thirteen_digit_EIK_checks_digits_9_to_12()
    {
        var body = Eik.Complete9("20000001") + "001";
        var check = Eik.CheckDigit(body.AsSpan(8, 4), [2, 7, 3, 5], [4, 9, 5, 7]);

        Assert.True(Eik.IsValid(body + check));
        Assert.False(Eik.IsValid(body + ((check + 1) % 10)));
    }

    [Fact]
    public void Demo_suppliers_have_valid_EIKs() =>
        Assert.All(PurchasingSeeder.Suppliers, s => Assert.True(Eik.IsValid(Eik.Complete9(s.EikPrefix))));
}

public class SupplierTests
{
    [Fact]
    public void New_supplier_is_active_with_trimmed_values()
    {
        var supplier = Supplier.Create("  Фарма Демо ООД ", " 200000015 ", 30).Value;

        Assert.Equal("Фарма Демо ООД", supplier.Name);
        Assert.Equal("200000015", supplier.Eik);
        Assert.True(supplier.IsActive);
    }

    [Theory]
    [InlineData("", "200000015", 30, "purchasing.supplier_name_required")]
    [InlineData("Демо", "123", 30, "purchasing.invalid_eik")]
    [InlineData("Демо", "200000015", -1, "purchasing.invalid_payment_term")]
    [InlineData("Демо", "200000015", 366, "purchasing.invalid_payment_term")]
    public void Invalid_supplier_is_rejected(string name, string eik, int days, string code) =>
        Assert.Equal(code, Supplier.Create(name, eik, days).Error.Code);
}

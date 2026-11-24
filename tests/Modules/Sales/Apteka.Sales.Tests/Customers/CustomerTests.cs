using Apteka.Sales.Domain.Customers;

namespace Apteka.Sales.Tests.Customers;

public class CustomerTests
{
    [Theory]
    [InlineData("7501011234", false)] // wrong check digit
    [InlineData("75010112", false)]
    [InlineData("75130112340", false)]
    [InlineData("7513011230", false)] // month 13
    [InlineData("7502301230", false)] // 30 February
    public void Invalid_numbers_are_refused(string egn, bool expected) => Assert.Equal(expected, Egn.IsValid(egn));

    [Fact]
    public void A_completed_number_is_valid_and_its_century_comes_from_the_month()
    {
        var born1975 = Egn.Complete("750101123");
        var born2002 = Egn.Complete("024307321");
        var born1899 = Egn.Complete("992312345");

        Assert.True(Egn.IsValid(born1975));
        Assert.Equal(new DateOnly(1975, 1, 1), Egn.BirthDate(born1975));
        Assert.Equal(new DateOnly(2002, 3, 7), Egn.BirthDate(born2002));
        Assert.Equal(new DateOnly(1899, 3, 12), Egn.BirthDate(born1899));
    }

    [Fact]
    public void Check_digit_10_counts_as_0()
    {
        // Find nine digits whose weighted sum gives 10 mod 11, and check the rule on them.
        var nine = Enumerable.Range(0, 1000).Select(i => $"750101{i:D3}").First(n => Weighted(n) % 11 == 10);
        Assert.Equal(nine + "0", Egn.Complete(nine));
        Assert.True(Egn.IsValid(nine + "0"));

        static int Weighted(string n) => new[] { 2, 4, 8, 5, 10, 9, 7, 3, 6 }.Select((w, i) => w * (n[i] - '0')).Sum();
    }

    [Fact]
    public void Customer_is_trimmed_and_checked()
    {
        var customer = Customer.Create(new CustomerDetails("  Демо Клиент  ", Egn.Complete("750101123"), " 0888 000 001 ", "  ")).Value;

        Assert.Equal(("Демо Клиент", "0888 000 001", (string?)null, true), (customer.Name, customer.Phone, customer.Note, customer.IsActive));
        Assert.Equal(CustomerErrors.NameRequired, Customer.Create(new CustomerDetails(" ", null, null, null)).Error);
        Assert.Equal(CustomerErrors.InvalidEgn, Customer.Create(new CustomerDetails("X", "7501011234", null, null)).Error);
        Assert.Equal(CustomerErrors.InvalidPhone, Customer.Create(new CustomerDetails("X", null, "088-abc", null)).Error);
        Assert.Null(Customer.Create(new CustomerDetails("X", "", null, null)).Value.Egn);
    }

    [Theory]
    [InlineData("750101123")]
    [InlineData("820512456")]
    [InlineData("900520789")]
    [InlineData("024307321")]
    public void Demo_seed_numbers_are_valid(string nine) => Assert.True(Egn.IsValid(Egn.Complete(nine)));
}

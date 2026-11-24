using Apteka.Sales.Domain.Sales;

namespace Apteka.Sales.Tests.Sales;

public class SaleTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);

    private static SaleLineSpec Line(string name = "Парацетамол", int quantity = 2, decimal price = 3.60m, decimal discount = 0m, decimal vat = 20m) =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), name, Guid.CreateVersion7(), "кутия", 20, quantity, price, discount, vat,
            [new SaleLineBatchSpec(Guid.CreateVersion7(), quantity * 20)]);

    [Fact]
    public void Total_and_VAT_are_the_sums_of_the_lines_and_cash_gives_change()
    {
        var sale = Sale.Create(Guid.CreateVersion7(), [Line(quantity: 2, price: 3.60m), Line(quantity: 1, price: 5.45m, vat: 9m)], PaymentMethod.Cash, 20m, null, Now).Value;

        Assert.Equal(12.65m, sale.Total);
        Assert.Equal(1.20m + 0.45m, sale.VatTotal);
        Assert.Equal(7.35m, sale.Change);
        Assert.Equal(SaleStatus.Completed, sale.Status);
        Assert.Equal(SaleType.Retail, sale.SaleType);
        Assert.Equal([1, 2], sale.Lines.Select(l => l.Position));
    }

    [Fact]
    public void Card_payment_has_no_cash_and_no_change()
    {
        var sale = Sale.Create(Guid.CreateVersion7(), [Line()], PaymentMethod.Card, null, null, Now).Value;

        Assert.Null(sale.CashReceived);
        Assert.Null(sale.Change);
    }

    [Fact]
    public void Not_enough_cash_is_rejected() =>
        Assert.Equal(SalesErrors.CashTooLow, Sale.Create(Guid.CreateVersion7(), [Line(quantity: 2, price: 3.60m)], PaymentMethod.Cash, 7.19m, null, Now).Error);

    [Fact]
    public void Invalid_lines_are_rejected()
    {
        Assert.Equal(SalesErrors.NoLines, Sale.Create(Guid.CreateVersion7(), [], PaymentMethod.Card, null, null, Now).Error);
        Assert.Equal("sales.quantity_positive", Sale.Create(Guid.CreateVersion7(), [Line(quantity: 0)], PaymentMethod.Card, null, null, Now).Error.Code);
        Assert.Equal("sales.invalid_discount", Sale.Create(Guid.CreateVersion7(), [Line(discount: 101)], PaymentMethod.Card, null, null, Now).Error.Code);
        Assert.Equal("sales.invalid_price", Sale.Create(Guid.CreateVersion7(), [Line(price: 0)], PaymentMethod.Card, null, null, Now).Error.Code);
    }

    [Fact]
    public void Sale_is_reversed_only_once_and_is_kept()
    {
        var sale = Sale.Create(Guid.CreateVersion7(), [Line()], PaymentMethod.Card, null, null, Now).Value;

        var reversal = sale.Reverse("  грешен артикул ", null, Now.AddMinutes(5));

        Assert.True(reversal.IsSuccess);
        Assert.Equal("грешен артикул", reversal.Value.Reason);
        Assert.Equal(sale.Id, reversal.Value.SaleId);
        Assert.Equal(SaleStatus.Reversed, sale.Status);
        Assert.Equal(7.20m, sale.Total);
        Assert.Equal(SalesErrors.AlreadyReversed, sale.Reverse(null, null, Now).Error);
    }
}

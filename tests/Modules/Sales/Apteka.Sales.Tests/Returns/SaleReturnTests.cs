using Apteka.Sales.Domain.Returns;
using Apteka.Sales.Domain.Sales;

namespace Apteka.Sales.Tests.Returns;

public class SaleReturnTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 24, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Early = Guid.CreateVersion7();
    private static readonly Guid Late = Guid.CreateVersion7();

    /// <summary>3 boxes at 3,33 € with 10 % discount (total 8,99), taken 1 box from the early batch and 2 from the late one.</summary>
    private static Sale NewSale() => Sale.Create(
        Guid.CreateVersion7(),
        [new SaleLineSpec(Guid.CreateVersion7(), Guid.CreateVersion7(), "Аналгин", Guid.CreateVersion7(), "кутия", 20, 3, 3.33m, 10m, 20m,
            [new SaleLineBatchSpec(Early, 20), new SaleLineBatchSpec(Late, 40)])],
        PaymentMethod.Card,
        null,
        null,
        Now).Value;

    private static Dictionary<Guid, int> Back(Sale sale, int quantity) => new() { [sale.Lines[0].Id] = quantity };

    [Fact]
    public void Partial_returns_add_up_to_the_line_total_to_the_cent_and_go_back_to_the_latest_batch_first()
    {
        var sale = NewSale();
        Assert.Equal(8.99m, sale.Total);

        var first = SaleReturn.Create(Guid.CreateVersion7(), sale, [], Back(sale, 1), " счупена ", null, Now).Value;
        var second = SaleReturn.Create(Guid.CreateVersion7(), sale, [first], Back(sale, 2), null, null, Now).Value;

        Assert.Equal(("счупена", 3.00m), (first.Reason, first.Total));
        Assert.Equal(5.99m, second.Total);
        Assert.Equal(sale.Lines[0].VatAmount, first.VatTotal + second.VatTotal);
        Assert.Equal([(Late, 20)], first.Lines[0].Batches.Select(b => (b.BatchId, b.BaseQuantity)));
        Assert.Equal([(Late, 20), (Early, 20)], second.Lines[0].Batches.Select(b => (b.BatchId, b.BaseQuantity)));
    }

    [Fact]
    public void More_than_is_left_nothing_and_a_reversed_sale_are_refused()
    {
        var sale = NewSale();
        var first = SaleReturn.Create(Guid.CreateVersion7(), sale, [], Back(sale, 2), null, null, Now).Value;

        Assert.Contains("най-много 1 кутия", SaleReturn.Create(Guid.CreateVersion7(), sale, [first], Back(sale, 2), null, null, Now).Error.Message);
        Assert.Equal(ReturnErrors.NothingToReturn, SaleReturn.Create(Guid.CreateVersion7(), sale, [], Back(sale, 0), null, null, Now).Error);
        Assert.Equal(ReturnErrors.UnknownLine, SaleReturn.Create(Guid.CreateVersion7(), sale, [], new Dictionary<Guid, int> { [Guid.CreateVersion7()] = 1 }, null, null, Now).Error);

        Assert.True(sale.Reverse(null, null, Now).IsSuccess);
        Assert.Equal(ReturnErrors.SaleReversed, SaleReturn.Create(Guid.CreateVersion7(), sale, [], Back(sale, 1), null, null, Now).Error);
    }
}

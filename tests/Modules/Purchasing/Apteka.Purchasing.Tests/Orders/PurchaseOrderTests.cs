using Apteka.Purchasing.Domain.Orders;

namespace Apteka.Purchasing.Tests.Orders;

public class PurchaseOrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid ProductA = Guid.CreateVersion7();
    private static readonly Guid BoxA = Guid.CreateVersion7();
    private static readonly Guid ProductB = Guid.CreateVersion7();
    private static readonly Guid BoxB = Guid.CreateVersion7();

    [Fact]
    public void A_line_taken_out_of_a_draft_is_kept_as_removed_and_comes_back_when_added_again()
    {
        var order = PurchaseOrder.Create(Guid.CreateVersion7(), " спешно ", [new(ProductA, BoxA, 20, 3), new(ProductB, BoxB, 10, 1)], null, Now).Value;

        Assert.True(order.Update(order.SupplierId, null, [new(ProductB, BoxB, 10, 2)]).IsSuccess);
        Assert.Equal([(ProductB, 2, 1)], order.Lines.Select(l => (l.ProductId, l.Quantity, l.Position)));
        Assert.Equal(2, order.AllLines.Count);

        Assert.True(order.Update(order.SupplierId, null, [new(ProductA, BoxA, 20, 5), new(ProductB, BoxB, 10, 2)]).IsSuccess);
        Assert.Equal(2, order.AllLines.Count);
        Assert.Equal([5, 2], order.Lines.Select(l => l.Quantity));
    }

    [Fact]
    public void Sent_order_no_longer_changes_and_bad_input_is_refused()
    {
        var order = PurchaseOrder.Create(Guid.CreateVersion7(), null, [new(ProductA, BoxA, 20, 3)], null, Now).Value;
        Assert.True(order.MarkSent(Now).IsSuccess);

        Assert.Equal(OrderErrors.NotDraft, order.Update(order.SupplierId, null, [new(ProductA, BoxA, 20, 1)]).Error);
        Assert.Equal(OrderErrors.NotDraft, order.MarkSent(Now).Error);
        Assert.Equal(OrderErrors.NoLines, PurchaseOrder.Create(Guid.CreateVersion7(), null, [new(ProductA, BoxA, 20, 0)], null, Now).Error);
        Assert.Equal("purchasing.order_quantity", PurchaseOrder.Create(Guid.CreateVersion7(), null, [new(ProductA, BoxA, 20, -1)], null, Now).Error.Code);
        Assert.Equal(OrderErrors.DuplicateProduct, PurchaseOrder.Create(Guid.CreateVersion7(), null, [new(ProductA, BoxA, 20, 1), new(ProductA, BoxA, 20, 2)], null, Now).Error);
    }
}

using Apteka.Inventory.Contracts;
using Apteka.Purchasing.Application.Consignments;
using Apteka.Purchasing.Application.Payments;

namespace Apteka.Purchasing.Tests.Consignments;

public class ConsignmentMathTests
{
    // 4 boxes of 10 at 3,3333 net a box; 1 box already given back.
    private static readonly ConsignmentLine Line = new(Guid.NewGuid(), Guid.NewGuid(), "B1", new DateOnly(2028, 1, 1), 10, 4, -1, 3.3333m);

    [Fact]
    public void Owes_for_what_is_neither_returned_nor_on_the_shelf()
    {
        var flow = new SourceLineFlow(Sold: 12, WrittenOff: 1, OnHand: 17);

        Assert.Equal(30, Line.ReceivedBaseQuantity);
        Assert.Equal(13, ConsignmentMath.ChargeableBaseQuantity(Line, flow));
        Assert.Equal(4.33m, ConsignmentMath.OwedNet(Line, flow)); // 3,3333 × 1,3
        Assert.Equal(5.20m, ConsignmentMath.Owed(Line, flow, 20m));
    }

    [Fact]
    public void Owes_nothing_before_anything_left_or_when_a_count_found_more()
    {
        Assert.Equal(30, ConsignmentMath.ChargeableBaseQuantity(Line, null)); // no batch: everything is gone
        Assert.Equal(0, ConsignmentMath.ChargeableBaseQuantity(Line, new SourceLineFlow(0, 0, 30)));
        Assert.Equal(0, ConsignmentMath.ChargeableBaseQuantity(Line, new SourceLineFlow(0, 0, 32)));
    }
}

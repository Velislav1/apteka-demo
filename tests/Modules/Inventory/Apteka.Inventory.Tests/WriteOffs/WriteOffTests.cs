using Apteka.Inventory.Domain.StockMovements;
using Apteka.Inventory.Domain.WriteOffs;
using Apteka.SharedKernel;

namespace Apteka.Inventory.Tests.WriteOffs;

public class WriteOffTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 24, 9, 0, 0, TimeSpan.Zero);

    private static WriteOffLineSpec Line(int quantity = 1, int factor = 20, decimal purchase = 0.2m) =>
        new(Guid.CreateVersion7(), "Аналгин", "кутия", factor, quantity, Guid.CreateVersion7(), "L1", new DateOnly(2026, 1, 1), purchase);

    [Fact]
    public void Lines_are_numbered_quantities_go_to_base_units_and_the_cost_is_in_cents()
    {
        var writeOff = WriteOff.Create(Guid.CreateVersion7(), "  счупени  ", [Line(2, 20, 0.2049m), Line(3, 1, 0.2049m)], null, Now).Value;

        Assert.Equal("счупени", writeOff.Reason);
        Assert.Equal([1, 2], writeOff.Lines.Select(l => l.Position));
        Assert.Equal([40, 3], writeOff.Lines.Select(l => l.BaseQuantity));
        Assert.Equal(Money.Of(8.20m) + Money.Of(0.61m), writeOff.PurchaseTotal);
    }

    [Fact]
    public void Empty_document_long_reason_and_zero_quantity_are_refused()
    {
        Assert.Equal(WriteOffErrors.NoLines, WriteOff.Create(Guid.CreateVersion7(), null, [], null, Now).Error);
        Assert.Equal(WriteOffErrors.ReasonTooLong, WriteOff.Create(Guid.CreateVersion7(), new string('x', 201), [Line()], null, Now).Error);
        Assert.Equal("inventory.write_off_quantity", WriteOff.Create(Guid.CreateVersion7(), null, [Line(), Line(0)], null, Now).Error.Code);
    }

    [Fact]
    public void Write_off_and_correction_movements_have_the_right_sign_and_kind()
    {
        var batch = Guid.CreateVersion7();

        var writeOff = StockMovement.WriteOff(batch, 5, "WriteOff", Guid.CreateVersion7(), null, Now).Value;
        var minus = StockMovement.Correction(batch, -3, "Correction", Guid.CreateVersion7(), null, Now).Value;

        Assert.Equal((-5, MovementKind.WriteOff), (writeOff.QuantityDelta, writeOff.Kind));
        Assert.Equal((-3, MovementKind.Correction), (minus.QuantityDelta, minus.Kind));
        Assert.True(StockMovement.Correction(batch, 0, "Correction", Guid.CreateVersion7(), null, Now).IsFailure);
    }
}

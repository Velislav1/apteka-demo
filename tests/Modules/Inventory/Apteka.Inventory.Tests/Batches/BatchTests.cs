using Apteka.Inventory.Domain.Batches;
using Apteka.Inventory.Domain.StockMovements;

namespace Apteka.Inventory.Tests.Batches;

public class BatchTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 24, 9, 0, 0, TimeSpan.Zero);

    private static BatchSpec Spec(int factor = 30, decimal purchasePerBase = 0.1374m, decimal unitSale = 10m, string batchNo = " L001 ") =>
        new(Guid.CreateVersion7(), batchNo, new DateOnly(2027, 5, 31), StockType.Retail, purchasePerBase, factor, unitSale, "Delivery", Guid.CreateVersion7(), Guid.CreateVersion7());

    [Fact]
    public void Prices_are_kept_per_base_unit_and_for_the_priced_unit()
    {
        var batch = Batch.Create(Spec(factor: 30, unitSale: 10m), Now).Value;

        Assert.Equal("L001", batch.BatchNo);
        Assert.Equal(0.3333m, batch.SalePrice);
        Assert.Equal(10m, batch.PricedUnitSalePrice);
        Assert.Equal(30, batch.PricedUnitFactor);
        Assert.Equal(0.1374m, batch.PurchasePrice);
    }

    [Fact]
    public void Batch_is_expired_only_after_its_expiry_date()
    {
        var batch = Batch.Create(Spec(), Now).Value;

        Assert.False(batch.IsExpiredOn(new DateOnly(2027, 5, 31)));
        Assert.True(batch.IsExpiredOn(new DateOnly(2027, 6, 1)));
    }

    [Theory]
    [InlineData(0, 0.1, 10, "L1", "inventory.invalid_factor")]
    [InlineData(1, -0.1, 10, "L1", "inventory.invalid_price")]
    [InlineData(1, 0.1, 0, "L1", "inventory.invalid_price")]
    [InlineData(1, 0.1, 10, "  ", "inventory.batch_no_required")]
    public void Invalid_batch_is_rejected(int factor, decimal purchase, decimal sale, string batchNo, string code) =>
        Assert.Equal(code, Batch.Create(Spec(factor, purchase, sale, batchNo), Now).Error.Code);

    [Fact]
    public void Receipt_movement_adds_a_positive_quantity_only()
    {
        var batch = Batch.Create(Spec(), Now).Value;

        var movement = StockMovement.Receipt(batch, 90, "Delivery", Guid.CreateVersion7(), null, Now).Value;
        Assert.Equal(90, movement.QuantityDelta);
        Assert.Equal(MovementKind.Delivery, movement.Kind);
        Assert.Equal(batch.Id, movement.BatchId);

        Assert.Equal(InventoryErrors.QuantityMustBePositive, StockMovement.Receipt(batch, 0, "Delivery", Guid.CreateVersion7(), null, Now).Error);
    }
}

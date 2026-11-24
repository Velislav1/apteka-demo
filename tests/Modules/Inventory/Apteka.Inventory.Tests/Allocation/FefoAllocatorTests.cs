using Apteka.Inventory.Domain.Allocation;

namespace Apteka.Inventory.Tests.Allocation;

public class FefoAllocatorTests
{
    private static readonly DateOnly Today = new(2026, 11, 24);
    private static readonly DateTimeOffset Received = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static BatchAvailability Batch(int quantity, int expiresInDays, int receivedDay = 0) =>
        new(Guid.CreateVersion7(), Today.AddDays(expiresInDays), Received.AddDays(receivedDay), quantity);

    [Fact]
    public void Nearest_expiry_is_issued_first()
    {
        var late = Batch(100, 300);
        var soon = Batch(100, 30);

        var allocation = FefoAllocator.Allocate([late, soon], 20, Today).Value;

        Assert.Equal([new BatchAllocation(soon.BatchId, 20)], allocation);
    }

    [Fact]
    public void A_line_is_split_over_batches_when_the_first_is_not_enough()
    {
        var soon = Batch(15, 30);
        var late = Batch(100, 300);

        var allocation = FefoAllocator.Allocate([late, soon], 40, Today).Value;

        Assert.Equal([new BatchAllocation(soon.BatchId, 15), new BatchAllocation(late.BatchId, 25)], allocation);
    }

    [Fact]
    public void Expired_batches_are_never_issued_but_one_expiring_today_is()
    {
        var expired = Batch(100, -1);
        var today = Batch(5, 0);

        Assert.Equal([new BatchAllocation(today.BatchId, 5)], FefoAllocator.Allocate([expired, today], 5, Today).Value);

        var tooMuch = FefoAllocator.Allocate([expired, today], 6, Today);
        Assert.Equal("inventory.insufficient_stock", tooMuch.Error.Code);
        Assert.Contains("5", tooMuch.Error.Message);
    }

    [Fact]
    public void Same_expiry_is_taken_in_the_order_received()
    {
        var older = Batch(10, 100, receivedDay: 1);
        var newer = Batch(10, 100, receivedDay: 2);

        Assert.Equal(older.BatchId, FefoAllocator.Allocate([newer, older], 5, Today).Value.Single().BatchId);
    }

    [Fact]
    public void Empty_batches_and_zero_quantity_are_handled()
    {
        Assert.Equal("inventory.insufficient_stock", FefoAllocator.Allocate([Batch(0, 100)], 1, Today).Error.Code);
        Assert.Equal("inventory.quantity_positive", FefoAllocator.Allocate([Batch(10, 100)], 0, Today).Error.Code);
    }
}

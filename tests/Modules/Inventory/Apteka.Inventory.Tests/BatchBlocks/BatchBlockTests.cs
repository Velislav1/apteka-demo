using Apteka.Inventory.Domain.Allocation;
using Apteka.Inventory.Domain.BatchBlocks;

namespace Apteka.Inventory.Tests.BatchBlocks;

public class BatchBlockTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 25);

    [Fact]
    public void Blocking_needs_a_reason_and_must_change_the_state()
    {
        var batch = Guid.CreateVersion7();

        Assert.Equal(BatchBlockErrors.ReasonRequired, BatchBlock.Create(batch, true, false, " ", null, Now).Error);
        Assert.Equal(BatchBlockErrors.AlreadyBlocked, BatchBlock.Create(batch, true, true, "ИАЛ", null, Now).Error);
        Assert.Equal(BatchBlockErrors.NotBlocked, BatchBlock.Create(batch, false, false, null, null, Now).Error);
        Assert.True(BatchBlock.Create(batch, false, true, null, null, Now).IsSuccess);
        Assert.Equal("ИАЛ 12/2026", BatchBlock.Create(batch, true, false, " ИАЛ 12/2026 ", null, Now).Value.Reason);
    }

    [Fact]
    public void Fefo_skips_a_blocked_batch_even_when_it_expires_first()
    {
        var blocked = new BatchAvailability(Guid.CreateVersion7(), Today.AddDays(10), Now, 20, IsBlocked: true);
        var later = new BatchAvailability(Guid.CreateVersion7(), Today.AddDays(300), Now, 20);

        var taken = Assert.Single(FefoAllocator.Allocate([blocked, later], 5, Today).Value);

        Assert.Equal(later.BatchId, taken.BatchId);
        Assert.True(FefoAllocator.Allocate([blocked], 1, Today).IsFailure);
    }
}

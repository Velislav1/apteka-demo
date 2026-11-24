using Apteka.Purchasing.Domain.Corrections;
using Apteka.Purchasing.Domain.Deliveries;
using Apteka.SharedKernel;

namespace Apteka.Purchasing.Tests.Corrections;

public class DeliveryCorrectionTests
{
    private static readonly DateOnly Today = new(2026, 11, 24);
    private static readonly DateTimeOffset Now = new(2026, 11, 24, 9, 0, 0, TimeSpan.Zero);

    private static Delivery Posted(bool post = true)
    {
        var delivery = Delivery.Create(Guid.CreateVersion7(), "1", Today, Now, null).Value;
        Assert.True(delivery.SetLines([
            new DeliveryLineSpec(null, Guid.CreateVersion7(), Guid.CreateVersion7(), 20, 3, 4.00m, 25m, 6.00m, "A", Today.AddYears(1), 10m),
            new DeliveryLineSpec(null, Guid.CreateVersion7(), Guid.CreateVersion7(), 1, 10, 0.50m, 25m, 0.75m, "B", Today.AddYears(1)),
        ]).IsSuccess);
        if (post)
        {
            Assert.True(delivery.Post(Today, Now).IsSuccess);
        }

        return delivery;
    }

    private static Result<DeliveryCorrection> Correct(Delivery delivery, IReadOnlyDictionary<Guid, int>? before = null, params (Guid, int)[] lines) =>
        DeliveryCorrection.Create(Guid.CreateVersion7(), delivery, " КИ-7 ", Today, null, lines, before ?? new Dictionary<Guid, int>(), null, Now);

    [Fact]
    public void Credit_note_lowers_the_total_at_the_net_price_and_skips_zero_lines()
    {
        var delivery = Posted();
        var correction = Correct(delivery, null, (delivery.Lines[0].Id, -1), (delivery.Lines[1].Id, 0)).Value;

        var line = Assert.Single(correction.Lines);
        Assert.Equal("КИ-7", correction.NoteNo);
        Assert.Equal((1, -20), (line.DeliveryLinePosition, line.BaseQuantityDelta));
        Assert.Equal(Money.Of(-3.60m), correction.PurchaseTotal); // 4,00 − 10 %
    }

    [Fact]
    public void Cannot_take_more_than_was_received_counting_earlier_corrections()
    {
        var delivery = Posted();
        var line = delivery.Lines[0].Id;

        Assert.True(Correct(delivery, null, (line, -3)).IsSuccess);
        Assert.Equal("purchasing.correction_below_zero", Correct(delivery, new Dictionary<Guid, int> { [line] = -2 }, (line, -2)).Error.Code);
        Assert.True(Correct(delivery, new Dictionary<Guid, int> { [line] = -2 }, (line, 5)).IsSuccess);
    }

    [Fact]
    public void Draft_unknown_line_and_empty_corrections_are_refused()
    {
        var draft = Posted(post: false);
        Assert.Equal(CorrectionErrors.DeliveryNotPosted, Correct(draft, null, (draft.Lines[0].Id, 1)).Error);

        var delivery = Posted();
        Assert.Equal(CorrectionErrors.NoLines, Correct(delivery, null, (delivery.Lines[0].Id, 0)).Error);
        Assert.Equal(CorrectionErrors.UnknownLine, Correct(delivery, null, (Guid.CreateVersion7(), 1)).Error);
        Assert.Equal(CorrectionErrors.DuplicateLine, Correct(delivery, null, (delivery.Lines[0].Id, 1), (delivery.Lines[0].Id, -1)).Error);
    }
}

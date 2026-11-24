using Apteka.Inventory.Domain.Stocktakings;
using Apteka.SharedKernel;

namespace Apteka.Inventory.Tests.Stocktakings;

public class StocktakingTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 24, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid BatchA = Guid.CreateVersion7();
    private static readonly Guid BatchB = Guid.CreateVersion7();
    private static readonly Guid BatchC = Guid.CreateVersion7();

    private static Stocktaking Open() => Stocktaking.Start(
        Guid.CreateVersion7(),
        "Всички стоки",
        [
            new StocktakingLineSpec(Guid.CreateVersion7(), "Ибупрофен", BatchB, "B", new DateOnly(2027, 1, 1), 20, 0.20m),
            new StocktakingLineSpec(Guid.CreateVersion7(), "Аналгин", BatchA, "A", new DateOnly(2027, 1, 1), 30, 0.25m),
            new StocktakingLineSpec(Guid.CreateVersion7(), "Аналгин", BatchC, "C", new DateOnly(2026, 6, 1), 5, 0.25m),
        ],
        null,
        Now).Value;

    [Fact]
    public void Protocol_is_sorted_by_name_then_expiry()
    {
        var stocktaking = Open();

        Assert.Equal(StocktakingStatus.Open, stocktaking.Status);
        Assert.Equal(["C", "A", "B"], stocktaking.Lines.Select(l => l.BatchNo));
        Assert.Equal([1, 2, 3], stocktaking.Lines.Select(l => l.Position));
    }

    [Fact]
    public void Closing_turns_counted_differences_from_the_stock_on_hand_into_corrections_and_skips_uncounted_lines()
    {
        var stocktaking = Open();
        var line = (string batch) => stocktaking.Lines.Single(l => l.BatchNo == batch).Id;
        Assert.True(stocktaking.Count(line("A"), 27).IsSuccess);
        Assert.True(stocktaking.Count(line("B"), 22).IsSuccess);

        // Batch A has 28 on hand now (2 were sold after the protocol was opened).
        var corrections = stocktaking.Close(new Dictionary<Guid, int> { [BatchA] = 28, [BatchB] = 20, [BatchC] = 5 }, null, Now).Value;

        Assert.Equal([(BatchA, -1), (BatchB, 2)], corrections.OrderBy(c => c.BatchId == BatchB));
        Assert.Equal(StocktakingStatus.Closed, stocktaking.Status);
        Assert.Null(stocktaking.Lines.Single(l => l.BatchNo == "C").Difference);
        Assert.Equal(Money.Of(-0.25m), stocktaking.Lines.Single(l => l.BatchNo == "A").DifferenceValue);
        Assert.Equal(StocktakingErrors.Closed, stocktaking.Count(line("C"), 1).Error);
        Assert.True(stocktaking.Close(new Dictionary<Guid, int>(), null, Now).IsFailure);
    }

    [Fact]
    public void Negative_count_unknown_line_and_an_empty_protocol_are_refused()
    {
        var stocktaking = Open();

        Assert.Equal(StocktakingErrors.NegativeCount, stocktaking.Count(stocktaking.Lines[0].Id, -1).Error);
        Assert.Equal(StocktakingErrors.UnknownLine, stocktaking.Count(Guid.CreateVersion7(), 1).Error);
        Assert.Equal(StocktakingErrors.NothingToCount, Stocktaking.Start(Guid.CreateVersion7(), "x", [], null, Now).Error);
    }
}

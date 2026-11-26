using Apteka.SharedKernel;

namespace Apteka.Inventory.Domain.PriceChanges;

/// <summary>
/// „Промяна на цени“ (Apteka Expert 2): new sale prices for batches in stock, with the old price kept on every line.
/// The batches get the new price; this document says who changed what, when and why. It never changes.
/// <see cref="Number"/> comes from a database sequence.
/// </summary>
public sealed class PriceChange : Entity<Guid>
{
    public const int MaxReasonLength = 200;

    private readonly List<PriceChangeLine> _lines = [];

    private PriceChange()
    {
    }

    private PriceChange(Guid id)
        : base(id)
    {
    }

    public long Number { get; private set; }

    public string? Reason { get; private set; }

    public Guid? UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<PriceChangeLine> Lines => _lines;

    /// <param name="lines">Only lines whose price really changes are kept.</param>
    public static Result<PriceChange> Create(string? reason, IReadOnlyList<PriceChangeLineSpec> lines, Guid? userId, DateTimeOffset at)
    {
        var trimmed = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (trimmed is { Length: > MaxReasonLength })
        {
            return Result.Failure<PriceChange>(PriceChangeErrors.ReasonTooLong);
        }

        var changed = lines.Where(l => l.NewPrice != l.OldPrice).ToList();
        if (changed.Count == 0)
        {
            return Result.Failure<PriceChange>(PriceChangeErrors.NothingChanged);
        }

        if (changed.Any(l => l.NewPrice <= 0))
        {
            return Result.Failure<PriceChange>(PriceChangeErrors.PriceMustBePositive);
        }

        var change = new PriceChange(Guid.CreateVersion7()) { Reason = trimmed, UserId = userId, CreatedAt = at };
        change._lines.AddRange(changed.Select(l => new PriceChangeLine(change.Id, l)));
        return Result.Success(change);
    }
}

public sealed class PriceChangeLine : Entity<Guid>
{
    private PriceChangeLine()
    {
    }

    internal PriceChangeLine(Guid priceChangeId, PriceChangeLineSpec spec)
        : base(Guid.CreateVersion7())
    {
        PriceChangeId = priceChangeId;
        BatchId = spec.BatchId;
        ProductId = spec.ProductId;
        PricedUnitFactor = spec.PricedUnitFactor;
        OldPrice = spec.OldPrice;
        NewPrice = spec.NewPrice;
    }

    public Guid PriceChangeId { get; private set; }

    public Guid BatchId { get; private set; }

    public Guid ProductId { get; private set; }

    public int PricedUnitFactor { get; private set; }

    /// <summary>Sale price with VAT of the priced unit before the change.</summary>
    public decimal OldPrice { get; private set; }

    public decimal NewPrice { get; private set; }
}

public sealed record PriceChangeLineSpec(Guid BatchId, Guid ProductId, int PricedUnitFactor, decimal OldPrice, decimal NewPrice);

/// <summary>Errors of „Промяна на цени“, shown to the user in Bulgarian.</summary>
public static class PriceChangeErrors
{
    public static readonly Error NothingChanged = new("inventory.price_change_nothing", "Няма променени цени.");
    public static readonly Error PriceMustBePositive = new("inventory.price_change_positive", "Новата цена трябва да е по-голяма от нула.");
    public static readonly Error ReasonTooLong = new("inventory.price_change_reason", $"Основанието е по-дълго от {PriceChange.MaxReasonLength} знака.");
    public static readonly Error UnknownBatch = new("inventory.price_change_batch", "Партидата не е намерена.");
}

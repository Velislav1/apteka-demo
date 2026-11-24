using Apteka.SharedKernel;

namespace Apteka.Inventory.Domain.Stocktakings;

public enum StocktakingStatus
{
    Open = 1,
    Closed = 2,
}

/// <summary>
/// „Инвентаризация“ as in Apteka Expert 2: a protocol of the batches with stock (all, one group or one shelf).
/// While it is open, its products cannot be sold, received or written off; „Намерено“ is typed per batch.
/// Closing it turns every difference into a correction movement; a closed protocol never changes.
/// <see cref="Number"/> comes from a database sequence.
/// </summary>
public sealed class Stocktaking : Entity<Guid>
{
    public const int MaxScopeLength = 200;

    private readonly List<StocktakingLine> _lines = [];

    private Stocktaking()
    {
        Scope = string.Empty;
    }

    private Stocktaking(Guid id)
        : base(id)
    {
        Scope = string.Empty;
    }

    public long Number { get; private set; }

    /// <summary>What was counted, for the list and the printout: „Всички стоки“, „Група: …“, „Рафт: …“.</summary>
    public string Scope { get; private set; }

    public StocktakingStatus Status { get; private set; }

    public Guid? StartedBy { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public Guid? ClosedBy { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public IReadOnlyList<StocktakingLine> Lines => _lines;

    public static Result<Stocktaking> Start(Guid id, string scope, IReadOnlyList<StocktakingLineSpec> lines, Guid? userId, DateTimeOffset at)
    {
        if (lines.Count == 0)
        {
            return Result.Failure<Stocktaking>(StocktakingErrors.NothingToCount);
        }

        var stocktaking = new Stocktaking(id)
        {
            Scope = scope.Length > MaxScopeLength ? scope[..MaxScopeLength] : scope,
            Status = StocktakingStatus.Open,
            StartedBy = userId,
            StartedAt = at,
        };

        var position = 0;
        foreach (var spec in lines.OrderBy(l => l.ProductName).ThenBy(l => l.ExpiryDate))
        {
            stocktaking._lines.Add(new StocktakingLine(id, ++position, spec));
        }

        return Result.Success(stocktaking);
    }

    /// <summary>„Намерено“ in base units; null clears it (the line is then left as it is when closing).</summary>
    public Result Count(Guid lineId, int? countedBaseQuantity)
    {
        if (Status != StocktakingStatus.Open)
        {
            return Result.Failure(StocktakingErrors.Closed);
        }

        if (countedBaseQuantity < 0)
        {
            return Result.Failure(StocktakingErrors.NegativeCount);
        }

        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null)
        {
            return Result.Failure(StocktakingErrors.UnknownLine);
        }

        line.CountedQuantity = countedBaseQuantity;
        return Result.Success();
    }

    /// <summary>
    /// Closes with the quantities on hand now (the batches are locked by the caller). Returns the corrections:
    /// counted − on hand, for counted lines that differ.
    /// </summary>
    public Result<IReadOnlyList<(Guid BatchId, int Delta)>> Close(IReadOnlyDictionary<Guid, int> onHand, Guid? userId, DateTimeOffset at)
    {
        if (Status != StocktakingStatus.Open)
        {
            return Result.Failure<IReadOnlyList<(Guid, int)>>(StocktakingErrors.Closed);
        }

        var corrections = new List<(Guid BatchId, int Delta)>();
        foreach (var line in _lines.Where(l => l.CountedQuantity is not null))
        {
            var delta = line.CountedQuantity!.Value - onHand.GetValueOrDefault(line.BatchId);
            line.Difference = delta;
            if (delta != 0)
            {
                corrections.Add((line.BatchId, delta));
            }
        }

        Status = StocktakingStatus.Closed;
        ClosedBy = userId;
        ClosedAt = at;
        return Result.Success<IReadOnlyList<(Guid, int)>>(corrections);
    }
}

/// <summary>One batch of the protocol. Names are copied so the protocol reads the same later.</summary>
public sealed class StocktakingLine : Entity<Guid>
{
    private StocktakingLine()
    {
        ProductName = string.Empty;
        BatchNo = string.Empty;
    }

    internal StocktakingLine(Guid stocktakingId, int position, StocktakingLineSpec spec)
        : base(Guid.CreateVersion7())
    {
        StocktakingId = stocktakingId;
        Position = position;
        ProductId = spec.ProductId;
        ProductName = spec.ProductName;
        BatchId = spec.BatchId;
        BatchNo = spec.BatchNo;
        ExpiryDate = spec.ExpiryDate;
        ExpectedQuantity = spec.ExpectedBaseQuantity;
        PurchasePrice = spec.PurchasePricePerBaseUnit;
    }

    public Guid StocktakingId { get; private set; }

    public int Position { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; }

    public Guid BatchId { get; private set; }

    public string BatchNo { get; private set; }

    public DateOnly ExpiryDate { get; private set; }

    /// <summary>„По документи“: the stock when the protocol was opened, in base units.</summary>
    public int ExpectedQuantity { get; private set; }

    /// <summary>„Намерено“, in base units; null — not counted yet.</summary>
    public int? CountedQuantity { get; internal set; }

    /// <summary>Set when closing: counted − on hand (+ surplus, − shortage).</summary>
    public int? Difference { get; internal set; }

    /// <summary>Purchase price of one base unit, without VAT, for the value of the difference.</summary>
    public decimal PurchasePrice { get; private set; }

    public Money DifferenceValue => Difference is { } d
        ? (d >= 0 ? UnitPrice.Of(PurchasePrice).Times(d) : -UnitPrice.Of(PurchasePrice).Times(-d))
        : Money.Zero;
}

public sealed record StocktakingLineSpec(
    Guid ProductId,
    string ProductName,
    Guid BatchId,
    string BatchNo,
    DateOnly ExpiryDate,
    int ExpectedBaseQuantity,
    decimal PurchasePricePerBaseUnit);

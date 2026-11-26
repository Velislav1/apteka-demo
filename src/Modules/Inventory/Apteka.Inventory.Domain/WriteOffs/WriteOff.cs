using Apteka.SharedKernel;

namespace Apteka.Inventory.Domain.WriteOffs;

/// <summary>
/// „Бракуване на стоки“: goods taken out of stock because they expired, broke or were spoiled. Saved only when it is
/// completed, together with its stock movements; never changed or deleted afterwards (rule 6).
/// <see cref="Number"/> comes from a database sequence.
/// </summary>
public sealed class WriteOff : Entity<Guid>
{
    public const int MaxReasonLength = 200;

    private readonly List<WriteOffLine> _lines = [];

    private WriteOff()
    {
    }

    private WriteOff(Guid id)
        : base(id)
    {
    }

    public long Number { get; private set; }

    public string? Reason { get; private set; }

    /// <summary>Брак, мостра, протокол or липси (Apteka Expert 2 has a menu item for each).</summary>
    public WriteOffKind Kind { get; private set; }

    /// <summary>For a protocol: who receives the goods (a hospital ward, a doctor, a care home …).</summary>
    public string? Recipient { get; private set; }

    public Guid? UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<WriteOffLine> Lines => _lines;

    /// <summary>What the written-off goods cost (purchase prices without VAT), in euro.</summary>
    public Money PurchaseTotal => _lines.Aggregate(Money.Zero, (sum, l) => sum + l.PurchaseTotal);

    public static Result<WriteOff> Create(
        Guid id,
        string? reason,
        IReadOnlyList<WriteOffLineSpec> lines,
        Guid? userId,
        DateTimeOffset at,
        WriteOffKind kind = WriteOffKind.WriteOff,
        string? recipient = null)
    {
        var to = string.IsNullOrWhiteSpace(recipient) ? null : recipient.Trim();
        if (!Enum.IsDefined(kind))
        {
            return Result.Failure<WriteOff>(WriteOffErrors.InvalidKind);
        }

        if (kind == WriteOffKind.Protocol && to is null)
        {
            return Result.Failure<WriteOff>(WriteOffErrors.RecipientRequired);
        }

        if (to is { Length: > MaxReasonLength })
        {
            return Result.Failure<WriteOff>(WriteOffErrors.ReasonTooLong);
        }

        if (lines.Count == 0)
        {
            return Result.Failure<WriteOff>(WriteOffErrors.NoLines);
        }

        var trimmed = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (trimmed is { Length: > MaxReasonLength })
        {
            return Result.Failure<WriteOff>(WriteOffErrors.ReasonTooLong);
        }

        var writeOff = new WriteOff(id) { Reason = trimmed, UserId = userId, CreatedAt = at, Kind = kind, Recipient = to };
        for (var i = 0; i < lines.Count; i++)
        {
            var spec = lines[i];
            if (spec.Quantity <= 0 || spec.UnitFactor < 1)
            {
                return Result.Failure<WriteOff>(WriteOffErrors.QuantityMustBePositive(i + 1));
            }

            writeOff._lines.Add(new WriteOffLine(id, i + 1, spec));
        }

        return Result.Success(writeOff);
    }
}

/// <summary>One product from one batch. Names are copied, so the document reads the same if the catalog changes.</summary>
public sealed class WriteOffLine : Entity<Guid>
{
    private WriteOffLine()
    {
        ProductName = string.Empty;
        UnitName = string.Empty;
        BatchNo = string.Empty;
    }

    internal WriteOffLine(Guid writeOffId, int position, WriteOffLineSpec spec)
        : base(Guid.CreateVersion7())
    {
        WriteOffId = writeOffId;
        Position = position;
        ProductId = spec.ProductId;
        ProductName = spec.ProductName;
        UnitName = spec.UnitName;
        UnitFactor = spec.UnitFactor;
        Quantity = spec.Quantity;
        BatchId = spec.BatchId;
        BatchNo = spec.BatchNo;
        ExpiryDate = spec.ExpiryDate;
        PurchasePrice = spec.PurchasePricePerBaseUnit;
    }

    public Guid WriteOffId { get; private set; }

    public int Position { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; }

    public string UnitName { get; private set; }

    public int UnitFactor { get; private set; }

    /// <summary>In <see cref="UnitName"/>.</summary>
    public int Quantity { get; private set; }

    public int BaseQuantity => Quantity * UnitFactor;

    public Guid BatchId { get; private set; }

    public string BatchNo { get; private set; }

    public DateOnly ExpiryDate { get; private set; }

    /// <summary>Purchase price of one base unit, without VAT (from the batch).</summary>
    public decimal PurchasePrice { get; private set; }

    public Money PurchaseTotal => UnitPrice.Of(PurchasePrice).Times(BaseQuantity);
}

public sealed record WriteOffLineSpec(
    Guid ProductId,
    string ProductName,
    string UnitName,
    int UnitFactor,
    int Quantity,
    Guid BatchId,
    string BatchNo,
    DateOnly ExpiryDate,
    decimal PurchasePricePerBaseUnit);

/// <summary>Why goods are taken out of stock without a sale.</summary>
public enum WriteOffKind
{
    /// <summary>Бракуване: expired, broken, spoiled.</summary>
    WriteOff = 1,

    /// <summary>Изписване за мостра: a sample (e.g. for the Drug Agency, a check).</summary>
    Sample = 2,

    /// <summary>Изписване с протокол: handed over to an institution or a doctor with a protocol.</summary>
    Protocol = 3,

    /// <summary>Изписване на липси: found missing (theft, error).</summary>
    Shortage = 4,
}

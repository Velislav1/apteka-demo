using Apteka.Purchasing.Domain.Deliveries;
using Apteka.SharedKernel;

namespace Apteka.Purchasing.Domain.Corrections;

/// <summary>
/// „Корекционна складова разписка“: corrects the quantities of a posted delivery (credit or debit note from the
/// supplier). + adds to a line, − takes from it. A posted delivery itself never changes; saved corrections never
/// change either (rule 6). <see cref="Number"/> comes from a database sequence.
/// </summary>
public sealed class DeliveryCorrection : Entity<Guid>
{
    public const int MaxNoteNoLength = 30;
    public const int MaxReasonLength = 200;

    private readonly List<DeliveryCorrectionLine> _lines = [];

    private DeliveryCorrection()
    {
    }

    private DeliveryCorrection(Guid id)
        : base(id)
    {
    }

    public long Number { get; private set; }

    public Guid DeliveryId { get; private set; }

    /// <summary>Number of the credit/debit note (кредитно/дебитно известие), if any.</summary>
    public string? NoteNo { get; private set; }

    public DateOnly NoteDate { get; private set; }

    public string? Reason { get; private set; }

    public Guid? UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<DeliveryCorrectionLine> Lines => _lines;

    /// <summary>Change of the amount to pay, without VAT (− for a credit note).</summary>
    public Money PurchaseTotal => _lines.Aggregate(Money.Zero, (sum, l) => sum + l.PurchaseTotal);

    /// <param name="corrected">What earlier corrections already changed per delivery line, in that line's unit.</param>
    public static Result<DeliveryCorrection> Create(
        Guid id,
        Delivery delivery,
        string? noteNo,
        DateOnly noteDate,
        string? reason,
        IReadOnlyList<(Guid DeliveryLineId, int QuantityDelta)> lines,
        IReadOnlyDictionary<Guid, int> corrected,
        Guid? userId,
        DateTimeOffset at)
    {
        if (delivery.Status != DeliveryStatus.Posted)
        {
            return Result.Failure<DeliveryCorrection>(CorrectionErrors.DeliveryNotPosted);
        }

        var changed = lines.Where(l => l.QuantityDelta != 0).ToList();
        if (changed.Count == 0)
        {
            return Result.Failure<DeliveryCorrection>(CorrectionErrors.NoLines);
        }

        var note = string.IsNullOrWhiteSpace(noteNo) ? null : noteNo.Trim();
        var why = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (note is { Length: > MaxNoteNoLength } || why is { Length: > MaxReasonLength })
        {
            return Result.Failure<DeliveryCorrection>(CorrectionErrors.TextTooLong);
        }

        if (changed.Select(l => l.DeliveryLineId).Distinct().Count() != changed.Count)
        {
            return Result.Failure<DeliveryCorrection>(CorrectionErrors.DuplicateLine);
        }

        var correction = new DeliveryCorrection(id)
        {
            DeliveryId = delivery.Id,
            NoteNo = note,
            NoteDate = noteDate,
            Reason = why,
            UserId = userId,
            CreatedAt = at,
        };

        var position = 0;
        foreach (var (lineId, delta) in changed)
        {
            var line = delivery.Lines.FirstOrDefault(l => l.Id == lineId);
            if (line is null)
            {
                return Result.Failure<DeliveryCorrection>(CorrectionErrors.UnknownLine);
            }

            if (line.Quantity + corrected.GetValueOrDefault(lineId) + delta < 0)
            {
                return Result.Failure<DeliveryCorrection>(CorrectionErrors.BelowZero(line.Position));
            }

            correction._lines.Add(new DeliveryCorrectionLine(id, ++position, line, delta));
        }

        return Result.Success(correction);
    }
}

/// <summary>A change of one delivery line, in that line's unit and at its net purchase price.</summary>
public sealed class DeliveryCorrectionLine : Entity<Guid>
{
    private DeliveryCorrectionLine()
    {
    }

    internal DeliveryCorrectionLine(Guid correctionId, int position, DeliveryLine line, int quantityDelta)
        : base(Guid.CreateVersion7())
    {
        CorrectionId = correctionId;
        Position = position;
        DeliveryLineId = line.Id;
        DeliveryLinePosition = line.Position;
        ProductId = line.ProductId;
        UnitFactor = line.UnitFactor;
        QuantityDelta = quantityDelta;
        UnitPurchasePrice = line.NetUnitPurchasePrice;
    }

    public Guid CorrectionId { get; private set; }

    public int Position { get; private set; }

    public Guid DeliveryLineId { get; private set; }

    /// <summary>„Ред N“ of the delivery, for messages and the printout.</summary>
    public int DeliveryLinePosition { get; private set; }

    public Guid ProductId { get; private set; }

    public int UnitFactor { get; private set; }

    /// <summary>+ or −, in the delivery line's unit.</summary>
    public int QuantityDelta { get; private set; }

    public int BaseQuantityDelta => QuantityDelta * UnitFactor;

    /// <summary>Net purchase price of one unit, without VAT (from the delivery line).</summary>
    public decimal UnitPurchasePrice { get; private set; }

    public Money PurchaseTotal => QuantityDelta >= 0
        ? UnitPrice.Of(UnitPurchasePrice).Times(QuantityDelta)
        : Money.Zero - UnitPrice.Of(UnitPurchasePrice).Times(-QuantityDelta);
}

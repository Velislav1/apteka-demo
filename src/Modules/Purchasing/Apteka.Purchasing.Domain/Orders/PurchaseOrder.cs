using Apteka.SharedKernel;

namespace Apteka.Purchasing.Domain.Orders;

public enum PurchaseOrderStatus
{
    /// <summary>Being prepared; lines can change.</summary>
    Draft = 1,

    /// <summary>Sent to the supplier (by phone, e-mail or the supplier's site); no longer changes.</summary>
    Sent = 2,
}

/// <summary>
/// „Заявка за доставка“ as in Apteka Expert 2: what to order from a supplier, proposed from the minimum stock or the
/// sales of the last days and then corrected by hand. Not a stock document: nothing moves until the delivery arrives.
/// <see cref="Number"/> comes from a database sequence.
/// </summary>
public sealed class PurchaseOrder : Entity<Guid>
{
    public const int MaxNoteLength = 200;

    private readonly List<PurchaseOrderLine> _lines = [];

    private PurchaseOrder()
    {
    }

    private PurchaseOrder(Guid id)
        : base(id)
    {
    }

    public long Number { get; private set; }

    public Guid SupplierId { get; private set; }

    public string? Note { get; private set; }

    public PurchaseOrderStatus Status { get; private set; }

    public Guid? UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }

    /// <summary>The lines in the order (removed ones are kept but not listed).</summary>
    public IReadOnlyList<PurchaseOrderLine> Lines => _lines.Where(l => !l.IsRemoved).OrderBy(l => l.Position).ToList();

    /// <summary>All lines, also removed ones (for persistence).</summary>
    public IReadOnlyList<PurchaseOrderLine> AllLines => _lines;

    public static Result<PurchaseOrder> Create(Guid supplierId, string? note, IReadOnlyList<PurchaseOrderLineSpec> lines, Guid? userId, DateTimeOffset at)
    {
        var order = new PurchaseOrder(Guid.CreateVersion7()) { Status = PurchaseOrderStatus.Draft, UserId = userId, CreatedAt = at };
        var result = order.Update(supplierId, note, lines);
        return result.IsFailure ? Result.Failure<PurchaseOrder>(result.Error) : Result.Success(order);
    }

    /// <summary>Replaces the supplier, note and lines of a draft.</summary>
    public Result Update(Guid supplierId, string? note, IReadOnlyList<PurchaseOrderLineSpec> lines)
    {
        if (Status != PurchaseOrderStatus.Draft)
        {
            return Result.Failure(OrderErrors.NotDraft);
        }

        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmed is { Length: > MaxNoteLength })
        {
            return Result.Failure(OrderErrors.NoteTooLong);
        }

        var wanted = lines.Where(l => l.Quantity != 0).ToList();
        if (wanted.Count == 0)
        {
            return Result.Failure(OrderErrors.NoLines);
        }

        for (var i = 0; i < wanted.Count; i++)
        {
            if (wanted[i].Quantity < 0 || wanted[i].UnitFactor < 1)
            {
                return Result.Failure(OrderErrors.InvalidQuantity(i + 1));
            }
        }

        if (wanted.Select(l => (l.ProductId, l.ProductUnitId)).Distinct().Count() != wanted.Count)
        {
            return Result.Failure(OrderErrors.DuplicateProduct);
        }

        SupplierId = supplierId;
        Note = trimmed;

        // Rule 6: a line taken out of a draft is marked removed, not deleted; the same product and unit brings it back.
        foreach (var line in _lines)
        {
            line.IsRemoved = true;
        }

        for (var i = 0; i < wanted.Count; i++)
        {
            var spec = wanted[i];
            var line = _lines.FirstOrDefault(l => l.ProductId == spec.ProductId && l.ProductUnitId == spec.ProductUnitId);
            if (line is null)
            {
                _lines.Add(new PurchaseOrderLine(Id, i + 1, spec));
            }
            else
            {
                line.Restore(i + 1, spec.Quantity);
            }
        }

        return Result.Success();
    }

    public Result MarkSent(DateTimeOffset at)
    {
        if (Status != PurchaseOrderStatus.Draft)
        {
            return Result.Failure(OrderErrors.NotDraft);
        }

        Status = PurchaseOrderStatus.Sent;
        SentAt = at;
        return Result.Success();
    }
}

public sealed class PurchaseOrderLine : Entity<Guid>
{
    private PurchaseOrderLine()
    {
    }

    internal PurchaseOrderLine(Guid orderId, int position, PurchaseOrderLineSpec spec)
        : base(Guid.CreateVersion7())
    {
        OrderId = orderId;
        Position = position;
        ProductId = spec.ProductId;
        ProductUnitId = spec.ProductUnitId;
        UnitFactor = spec.UnitFactor;
        Quantity = spec.Quantity;
    }

    public Guid OrderId { get; private set; }

    public int Position { get; private set; }

    public Guid ProductId { get; private set; }

    public Guid ProductUnitId { get; private set; }

    public int UnitFactor { get; private set; }

    /// <summary>In <see cref="ProductUnitId"/>.</summary>
    public int Quantity { get; private set; }

    public bool IsRemoved { get; internal set; }

    internal void Restore(int position, int quantity)
    {
        Position = position;
        Quantity = quantity;
        IsRemoved = false;
    }
}

public sealed record PurchaseOrderLineSpec(Guid ProductId, Guid ProductUnitId, int UnitFactor, int Quantity);

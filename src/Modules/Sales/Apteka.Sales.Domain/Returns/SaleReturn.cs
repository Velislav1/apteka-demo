using Apteka.Sales.Domain.Pricing;
using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;

namespace Apteka.Sales.Domain.Returns;

/// <summary>
/// „Корекция / рекламация / кредитно известие“ to a sale, as in Apteka Expert 2: part of a sale is given back
/// (always a minus), the money is refunded and the goods go back into the batches they came from.
/// The sale itself never changes; a whole sale is undone with a reversal (сторно) instead.
/// <see cref="Number"/> comes from a database sequence.
/// </summary>
public sealed class SaleReturn : Entity<Guid>
{
    public const int MaxReasonLength = 200;

    private readonly List<SaleReturnLine> _lines = [];

    private SaleReturn()
    {
    }

    private SaleReturn(Guid id)
        : base(id)
    {
    }

    public long Number { get; private set; }

    public Guid SaleId { get; private set; }

    public string? Reason { get; private set; }

    public Guid? UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Refunded, VAT included (a positive amount).</summary>
    public decimal Total { get; private set; }

    public decimal VatTotal { get; private set; }

    public IReadOnlyList<SaleReturnLine> Lines => _lines;

    /// <param name="earlier">Returns already made on this sale.</param>
    /// <param name="quantities">Sale line id → quantity to give back, in the line's unit (0 lines are ignored).</param>
    public static Result<SaleReturn> Create(
        Guid id,
        Sale sale,
        IReadOnlyList<SaleReturn> earlier,
        IReadOnlyDictionary<Guid, int> quantities,
        string? reason,
        Guid? userId,
        DateTimeOffset at)
    {
        if (sale.Status == SaleStatus.Reversed)
        {
            return Result.Failure<SaleReturn>(ReturnErrors.SaleReversed);
        }

        var trimmed = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (trimmed is { Length: > MaxReasonLength })
        {
            return Result.Failure<SaleReturn>(SalesErrors.ReasonTooLong);
        }

        var wanted = quantities.Where(q => q.Value != 0).ToList();
        if (wanted.Count == 0)
        {
            return Result.Failure<SaleReturn>(ReturnErrors.NothingToReturn);
        }

        var previous = earlier.SelectMany(r => r.Lines).ToList();
        var saleReturn = new SaleReturn(id) { SaleId = sale.Id, Reason = trimmed, UserId = userId, CreatedAt = at };
        foreach (var (lineId, quantity) in wanted)
        {
            var line = sale.Lines.FirstOrDefault(l => l.Id == lineId);
            if (line is null)
            {
                return Result.Failure<SaleReturn>(ReturnErrors.UnknownLine);
            }

            var before = previous.Where(p => p.SaleLineId == lineId).ToList();
            var left = line.Quantity - before.Sum(p => p.Quantity);
            if (quantity < 0 || quantity > left)
            {
                return Result.Failure<SaleReturn>(ReturnErrors.TooMany(line.ProductName, left, line.UnitName));
            }

            // The last part of a line refunds exactly what is left of it, so the cents always add up.
            var lastPart = quantity == left;
            var amount = lastPart ? line.LineTotal - before.Sum(p => p.Amount) : SalePricing.Round(line.LineTotal * quantity / line.Quantity);
            var vat = lastPart ? line.VatAmount - before.Sum(p => p.VatAmount) : SalePricing.Round(line.VatAmount * quantity / line.Quantity);

            saleReturn._lines.Add(new SaleReturnLine(id, line, quantity, amount, vat, BatchesToPutBack(line, before, quantity * line.UnitFactor)));
        }

        saleReturn.Total = saleReturn._lines.Sum(l => l.Amount);
        saleReturn.VatTotal = saleReturn._lines.Sum(l => l.VatAmount);
        return Result.Success(saleReturn);
    }

    /// <summary>Back into the line's batches, the latest expiry first, never more than each batch gave.</summary>
    private static List<(Guid BatchId, int BaseQuantity)> BatchesToPutBack(SaleLine line, List<SaleReturnLine> before, int baseQuantity)
    {
        var alreadyBack = before.SelectMany(p => p.Batches).GroupBy(b => b.BatchId).ToDictionary(g => g.Key, g => g.Sum(b => b.BaseQuantity));
        var result = new List<(Guid, int)>();
        var left = baseQuantity;
        foreach (var batch in line.Batches.OrderByDescending(b => b.Position))
        {
            var room = batch.BaseQuantity - alreadyBack.GetValueOrDefault(batch.BatchId);
            var take = Math.Min(room, left);
            if (take > 0)
            {
                result.Add((batch.BatchId, take));
                left -= take;
            }
        }

        return result;
    }
}

public sealed class SaleReturnLine : Entity<Guid>
{
    private readonly List<SaleReturnLineBatch> _batches = [];

    private SaleReturnLine()
    {
    }

    internal SaleReturnLine(Guid returnId, SaleLine line, int quantity, decimal amount, decimal vat, List<(Guid BatchId, int BaseQuantity)> batches)
        : base(Guid.CreateVersion7())
    {
        ReturnId = returnId;
        SaleLineId = line.Id;
        Quantity = quantity;
        Amount = amount;
        VatAmount = vat;
        _batches.AddRange(batches.Select(b => new SaleReturnLineBatch(Id, b.BatchId, b.BaseQuantity)));
    }

    public Guid ReturnId { get; private set; }

    public Guid SaleLineId { get; private set; }

    /// <summary>Given back, in the sale line's unit.</summary>
    public int Quantity { get; private set; }

    /// <summary>Refunded for this line, VAT included.</summary>
    public decimal Amount { get; private set; }

    public decimal VatAmount { get; private set; }

    public IReadOnlyList<SaleReturnLineBatch> Batches => _batches;
}

/// <summary>Where the returned units went back, in base units.</summary>
public sealed class SaleReturnLineBatch : Entity<Guid>
{
    private SaleReturnLineBatch()
    {
    }

    internal SaleReturnLineBatch(Guid returnLineId, Guid batchId, int baseQuantity)
        : base(Guid.CreateVersion7())
    {
        ReturnLineId = returnLineId;
        BatchId = batchId;
        BaseQuantity = baseQuantity;
    }

    public Guid ReturnLineId { get; private set; }

    public Guid BatchId { get; private set; }

    public int BaseQuantity { get; private set; }
}

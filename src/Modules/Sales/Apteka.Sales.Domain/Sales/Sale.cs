using Apteka.Sales.Domain.Pricing;
using Apteka.Sales.Domain.Reversals;
using Apteka.SharedKernel;

namespace Apteka.Sales.Domain.Sales;

/// <summary>
/// A completed sale at the till. Never changed or deleted afterwards; a mistake is undone with a reversal (сторно).
/// <see cref="Number"/> comes from a database sequence when the sale is saved.
/// </summary>
public sealed class Sale : Entity<Guid>
{
    private readonly List<SaleLine> _lines = [];

    private Sale()
    {
    }

    private Sale(Guid id)
        : base(id)
    {
    }

    public long Number { get; private set; }

    public SaleType SaleType { get; private set; }

    public Guid? UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public SaleStatus Status { get; private set; }

    /// <summary>What the customer pays, VAT included.</summary>
    public decimal Total { get; private set; }

    public decimal VatTotal { get; private set; }

    public decimal? CashReceived { get; private set; }

    public decimal? Change => CashReceived - Total;

    public IReadOnlyList<SaleLine> Lines => _lines;

    public static Result<Sale> Create(
        Guid id,
        IReadOnlyList<SaleLineSpec> lines,
        PaymentMethod paymentMethod,
        decimal? cashReceived,
        Guid? userId,
        DateTimeOffset at)
    {
        if (lines.Count == 0)
        {
            return Result.Failure<Sale>(SalesErrors.NoLines);
        }

        foreach (var line in lines)
        {
            if (line.Quantity <= 0)
            {
                return Result.Failure<Sale>(SalesErrors.QuantityMustBePositive(line.ProductName));
            }

            if (line.DiscountPercent is < 0 or > 100)
            {
                return Result.Failure<Sale>(SalesErrors.InvalidDiscount(line.ProductName));
            }

            if (line.UnitPrice <= 0)
            {
                return Result.Failure<Sale>(SalesErrors.InvalidPrice(line.ProductName));
            }
        }

        var sale = new Sale(id)
        {
            SaleType = SaleType.Retail,
            UserId = userId,
            CreatedAt = at,
            PaymentMethod = paymentMethod,
            Status = SaleStatus.Completed,
        };

        for (var i = 0; i < lines.Count; i++)
        {
            var spec = lines[i];
            var amounts = SalePricing.Line(spec.UnitPrice, spec.Quantity, spec.DiscountPercent, spec.VatRate);
            sale._lines.Add(new SaleLine(id, i + 1, spec, amounts));
        }

        sale.Total = sale._lines.Sum(l => l.LineTotal);
        sale.VatTotal = sale._lines.Sum(l => l.VatAmount);

        switch (paymentMethod)
        {
            case PaymentMethod.Cash when cashReceived is null || SalePricing.Round(cashReceived.Value) < sale.Total:
                return Result.Failure<Sale>(SalesErrors.CashTooLow);
            case PaymentMethod.Cash:
                sale.CashReceived = SalePricing.Round(cashReceived.Value);
                break;
            case PaymentMethod.Card:
                sale.CashReceived = null;
                break;
            default:
                return Result.Failure<Sale>(SalesErrors.InvalidPayment);
        }

        return Result.Success(sale);
    }

    /// <summary>Marks the whole sale as reversed. The caller puts the stock back in the same transaction.</summary>
    public Result<Reversal> Reverse(string? reason, Guid? userId, DateTimeOffset at)
    {
        if (Status == SaleStatus.Reversed)
        {
            return Result.Failure<Reversal>(SalesErrors.AlreadyReversed);
        }

        var reversal = Reversal.Create(Id, reason, userId, at);
        if (reversal.IsFailure)
        {
            return reversal;
        }

        Status = SaleStatus.Reversed;
        return reversal;
    }
}

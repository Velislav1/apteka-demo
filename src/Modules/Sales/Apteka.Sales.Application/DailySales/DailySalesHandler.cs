using Apteka.Sales.Application.Common;
using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;

namespace Apteka.Sales.Application.DailySales;

/// <summary>„Продажби за деня“: the sales of one Sofia calendar day, with cash and card totals (reversed sales excluded).</summary>
public sealed class DailySalesHandler(ISaleRepository sales)
{
    public async Task<DailySalesView> HandleAsync(DateOnly day, CancellationToken cancellationToken = default)
    {
        var from = StartOfDayUtc(day);
        var to = StartOfDayUtc(day.AddDays(1));
        var list = await sales.ListAsync(from, to, cancellationToken);

        var completed = list.Where(s => s.Status == SaleStatus.Completed).ToList();
        return new DailySalesView(
            day,
            list.OrderByDescending(s => s.Number)
                .Select(s => new DailySaleItem(s.Id, s.Number, s.CreatedAt, s.Lines.Count, s.Total, s.PaymentMethod, s.Status, s.UserId))
                .ToList(),
            completed.Where(s => s.PaymentMethod == PaymentMethod.Cash).Sum(s => s.Total),
            completed.Where(s => s.PaymentMethod == PaymentMethod.Card).Sum(s => s.Total));
    }

    private static DateTimeOffset StartOfDayUtc(DateOnly day) => BulgarianTime.StartOfDayUtc(day);
}

/// <summary>One sale with its lines, for the details under „Продажби за деня“.</summary>
public sealed class GetSaleHandler(ISaleRepository sales)
{
    public async Task<SaleView?> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await sales.GetAsync(id, cancellationToken);
        return sale is null
            ? null
            : new SaleView(
                sale.Id,
                sale.Number,
                sale.CreatedAt,
                sale.PaymentMethod,
                sale.Status,
                sale.Total,
                sale.VatTotal,
                sale.CashReceived,
                sale.Lines.OrderBy(l => l.Position)
                    .Select(l => new SaleLineView(l.ProductName, l.UnitName, l.Quantity, l.UnitPrice, l.DiscountPercent, l.LineTotal))
                    .ToList());
    }
}

public sealed record DailySalesView(DateOnly Day, IReadOnlyList<DailySaleItem> Sales, decimal CashTotal, decimal CardTotal)
{
    public decimal Total => CashTotal + CardTotal;
}

public sealed record DailySaleItem(Guid Id, long Number, DateTimeOffset CreatedAt, int LineCount, decimal Total, PaymentMethod PaymentMethod, SaleStatus Status, Guid? UserId = null);

public sealed record SaleView(
    Guid Id,
    long Number,
    DateTimeOffset CreatedAt,
    PaymentMethod PaymentMethod,
    SaleStatus Status,
    decimal Total,
    decimal VatTotal,
    decimal? CashReceived,
    IReadOnlyList<SaleLineView> Lines);

public sealed record SaleLineView(string ProductName, string UnitName, int Quantity, decimal UnitPrice, decimal DiscountPercent, decimal LineTotal);

using Apteka.Sales.Application.Common;
using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;

namespace Apteka.Sales.Application.DailySales;

/// <summary>
/// „Продажби за деня“: the sales of one Sofia calendar day, with cash and card totals (reversed sales excluded,
/// refunds of that day's corrections subtracted from the way the sale was paid).
/// </summary>
public sealed class DailySalesHandler(ISaleRepository sales, Customers.ICustomerRepository customers)
{
    public async Task<DailySalesView> HandleAsync(DateOnly day, CancellationToken cancellationToken = default)
    {
        var from = StartOfDayUtc(day);
        var to = StartOfDayUtc(day.AddDays(1));
        var list = await sales.ListAsync(from, to, cancellationToken);

        var completed = list.Where(s => s.Status == SaleStatus.Completed).ToList();
        var returns = await sales.ListReturnsAsync(from, to, cancellationToken);
        var names = await customers.GetNamesAsync([.. list.Where(s => s.CustomerId is not null).Select(s => s.CustomerId!.Value).Distinct()], cancellationToken);
        return new DailySalesView(
            day,
            list.OrderByDescending(s => s.Number)
                .Select(s => new DailySaleItem(s.Id, s.Number, s.CreatedAt, s.Lines.Count, s.Total, s.PaymentMethod, s.Status, s.UserId,
                    s.CustomerId is { } c ? names.GetValueOrDefault(c) : null))
                .ToList(),
            completed.Where(s => s.PaymentMethod == PaymentMethod.Cash).Sum(s => s.Total)
                - returns.Where(r => r.PaymentMethod == PaymentMethod.Cash).Sum(r => r.Return.Total),
            completed.Where(s => s.PaymentMethod == PaymentMethod.Card).Sum(s => s.Total)
                - returns.Where(r => r.PaymentMethod == PaymentMethod.Card).Sum(r => r.Return.Total),
            returns.OrderByDescending(r => r.Return.Number)
                .Select(r => new DailyReturnItem(r.Return.Id, r.Return.Number, r.Return.CreatedAt, r.Return.SaleId, r.SaleNumber, r.Return.Total, r.PaymentMethod, r.Return.Reason))
                .ToList());
    }

    private static DateTimeOffset StartOfDayUtc(DateOnly day) => BulgarianTime.StartOfDayUtc(day);
}

/// <summary>One sale with its lines, what was given back of each, and its corrections.</summary>
public sealed class GetSaleHandler(ISaleRepository sales, Customers.ICustomerRepository customers)
{
    public async Task<SaleView?> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await sales.GetAsync(id, cancellationToken);
        if (sale is null)
        {
            return null;
        }

        var returns = await sales.ListReturnsAsync(id, cancellationToken);
        var returned = returns.SelectMany(r => r.Lines).GroupBy(l => l.SaleLineId).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
        var customer = sale.CustomerId is { } customerId ? (await customers.GetNamesAsync([customerId], cancellationToken)).GetValueOrDefault(customerId) : null;
        return new SaleView(
            sale.Id,
            sale.Number,
            sale.CreatedAt,
            sale.PaymentMethod,
            sale.Status,
            sale.Total,
            sale.VatTotal,
            sale.CashReceived,
            sale.Lines.OrderBy(l => l.Position)
                .Select(l => new SaleLineView(l.ProductName, l.UnitName, l.Quantity, l.UnitPrice, l.DiscountPercent, l.LineTotal, l.Id, returned.GetValueOrDefault(l.Id)))
                .ToList(),
            sale.UserId,
            sale.CustomerId,
            customer,
            returns.Select(r => new SaleReturnItem(r.Id, r.Number, r.CreatedAt, r.Total, r.Reason)).ToList());
    }
}

/// <summary>„Търсене по №“: the sale with this number, on any day.</summary>
public sealed class FindSaleHandler(ISaleRepository sales)
{
    public async Task<Guid?> HandleAsync(long number, CancellationToken cancellationToken = default) =>
        (await sales.FindByNumberAsync(number, cancellationToken))?.Id;
}

public sealed record DailySalesView(DateOnly Day, IReadOnlyList<DailySaleItem> Sales, decimal CashTotal, decimal CardTotal, IReadOnlyList<DailyReturnItem>? Returns = null)
{
    public decimal Total => CashTotal + CardTotal;
}

public sealed record DailySaleItem(Guid Id, long Number, DateTimeOffset CreatedAt, int LineCount, decimal Total, PaymentMethod PaymentMethod, SaleStatus Status, Guid? UserId = null, string? CustomerName = null);

/// <summary>A correction made that day (the refund is a positive amount).</summary>
public sealed record DailyReturnItem(Guid Id, long Number, DateTimeOffset CreatedAt, Guid SaleId, long SaleNumber, decimal Total, PaymentMethod PaymentMethod, string? Reason);

public sealed record SaleReturnItem(Guid Id, long Number, DateTimeOffset CreatedAt, decimal Total, string? Reason);

public sealed record SaleView(
    Guid Id,
    long Number,
    DateTimeOffset CreatedAt,
    PaymentMethod PaymentMethod,
    SaleStatus Status,
    decimal Total,
    decimal VatTotal,
    decimal? CashReceived,
    IReadOnlyList<SaleLineView> Lines,
    Guid? UserId = null,
    Guid? CustomerId = null,
    string? CustomerName = null,
    IReadOnlyList<SaleReturnItem>? Returns = null)
{
    /// <summary>What the customer finally paid: the total less the refunds.</summary>
    public decimal NetTotal => Total - (Returns?.Sum(r => r.Total) ?? 0m);
}

/// <param name="ReturnedQuantity">Given back so far with corrections, in the line's unit.</param>
public sealed record SaleLineView(
    string ProductName,
    string UnitName,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal LineTotal,
    Guid Id = default,
    int ReturnedQuantity = 0);

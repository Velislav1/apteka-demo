using Apteka.Inventory.Contracts;
using Apteka.Sales.Application.Common;
using Apteka.Sales.Domain.Pricing;
using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;

namespace Apteka.Sales.Application.DailySales;

/// <summary>
/// „Дневен опис на продажбите“ (as in Apteka Expert 2): every sale of a Sofia day with its lines, and the profit:
/// revenue without VAT minus the purchase cost of the batches the goods were taken from. Reversed sales are listed
/// but not counted.
/// </summary>
public sealed class DailyReportHandler(ISaleRepository sales, IStockQueries stock)
{
    public async Task<DailyReportView> HandleAsync(DateOnly day, CancellationToken cancellationToken = default)
    {
        var list = await sales.ListAsync(StartOfDayUtc(day), StartOfDayUtc(day.AddDays(1)), cancellationToken);
        var batchIds = list.SelectMany(s => s.Lines).SelectMany(l => l.Batches).Select(b => b.BatchId).Distinct().ToList();
        var prices = await stock.GetPurchasePricesAsync(batchIds, cancellationToken);

        var rows = list
            .OrderBy(s => s.Number)
            .Select(s =>
            {
                var lines = s.Lines.OrderBy(l => l.Position).Select(l =>
                {
                    var cost = SalePricing.Round(l.Batches.Sum(b => b.BaseQuantity * prices.GetValueOrDefault(b.BatchId)));
                    return new ReportLine(l.ProductName, l.UnitName, l.Quantity, l.UnitPrice, l.DiscountPercent, l.LineTotal, l.LineTotal - l.VatAmount, cost);
                }).ToList();
                return new ReportSale(s.Id, s.Number, s.CreatedAt, s.UserId, s.PaymentMethod, s.Status, lines);
            })
            .ToList();

        return new DailyReportView(day, rows);
    }

    private static DateTimeOffset StartOfDayUtc(DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, BulgarianTime.Zone.GetUtcOffset(local)).ToUniversalTime();
    }
}

public sealed record DailyReportView(DateOnly Day, IReadOnlyList<ReportSale> Sales)
{
    private IEnumerable<ReportSale> Counted => Sales.Where(s => s.Status == SaleStatus.Completed);

    public decimal Total => Counted.Sum(s => s.Total);

    public decimal NetRevenue => Counted.Sum(s => s.NetRevenue);

    public decimal Cost => Counted.Sum(s => s.Cost);

    public decimal Profit => NetRevenue - Cost;

    public decimal? ProfitPercent => ReportSale.Percent(Profit, Cost);
}

public sealed record ReportSale(
    Guid Id,
    long Number,
    DateTimeOffset CreatedAt,
    Guid? UserId,
    PaymentMethod PaymentMethod,
    SaleStatus Status,
    IReadOnlyList<ReportLine> Lines)
{
    public decimal Total => Lines.Sum(l => l.Total);

    public decimal NetRevenue => Lines.Sum(l => l.NetRevenue);

    public decimal Cost => Lines.Sum(l => l.Cost);

    public decimal Profit => NetRevenue - Cost;

    public decimal? ProfitPercent => Percent(Profit, Cost);

    /// <summary>Markup on cost, as pharmacies usually count it; null when there is no cost.</summary>
    internal static decimal? Percent(decimal profit, decimal cost) =>
        cost == 0 ? null : decimal.Round(profit / cost * 100m, 1, MidpointRounding.AwayFromZero);
}

/// <param name="NetRevenue">Line total without VAT.</param>
/// <param name="Cost">Purchase cost of the batches, without VAT.</param>
public sealed record ReportLine(string ProductName, string UnitName, int Quantity, decimal UnitPrice, decimal DiscountPercent, decimal Total, decimal NetRevenue, decimal Cost);

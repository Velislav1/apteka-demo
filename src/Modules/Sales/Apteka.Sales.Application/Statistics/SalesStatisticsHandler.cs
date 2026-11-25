using Apteka.Catalog.Contracts;
using Apteka.Inventory.Contracts;
using Apteka.Sales.Application.Common;
using Apteka.Sales.Application.Customers;
using Apteka.Sales.Domain.Pricing;
using Apteka.Sales.Domain.Sales;
using Apteka.SharedKernel;

namespace Apteka.Sales.Application.Statistics;

/// <summary>What a row of „Статистика на продажбите“ groups by (as in Apteka Expert 2).</summary>
public enum StatisticsView
{
    Products = 1,
    Groups = 2,
    Users = 3,
    Customers = 4,
}

/// <summary>
/// „Статистика на продажбите“ for a period (Sofia dates, both included): quantity, revenue with and without VAT, purchase
/// cost of the batches sold and profit, by product, group, user or customer. Reversed sales are left out and corrections
/// made in the period are subtracted. By product the rows also get an ABC class: A — the products that make the first
/// 80 % of the revenue, B — up to 95 %, C — the rest.
/// </summary>
public sealed class SalesStatisticsHandler(ISaleRepository sales, IStockQueries stock, ICatalogLookup catalog, ICustomerRepository customers)
{
    public async Task<IReadOnlyList<StatisticsRow>> HandleAsync(DateOnly from, DateOnly to, StatisticsView view, CancellationToken cancellationToken = default)
    {
        var fromUtc = BulgarianTime.StartOfDayUtc(from);
        var toUtc = BulgarianTime.StartOfDayUtc(to.AddDays(1));
        var sold = (await sales.ListAsync(fromUtc, toUtc, cancellationToken)).Where(s => s.Status == SaleStatus.Completed).ToList();
        var returns = await sales.ListReturnsAsync(fromUtc, toUtc, cancellationToken);

        // Sale lines of the returns may belong to sales of earlier days.
        var lines = sold.SelectMany(s => s.Lines.Select(l => (Sale: s, Line: l))).ToDictionary(x => x.Line.Id);
        foreach (var saleId in returns.Select(r => r.Return.SaleId).Distinct().Where(id => sold.All(s => s.Id != id)))
        {
            if (await sales.GetAsync(saleId, cancellationToken) is { } earlier)
            {
                foreach (var line in earlier.Lines)
                {
                    lines[line.Id] = (earlier, line);
                }
            }
        }

        var batchIds = sold.SelectMany(s => s.Lines).SelectMany(l => l.Batches).Select(b => b.BatchId)
            .Concat(returns.SelectMany(r => r.Return.Lines).SelectMany(l => l.Batches).Select(b => b.BatchId))
            .Distinct().ToList();
        var prices = await stock.GetPurchasePricesAsync(batchIds, cancellationToken);

        var entries = new List<Entry>();
        foreach (var (sale, line) in sold.SelectMany(s => s.Lines.Select(l => (s, l))))
        {
            entries.Add(new Entry(sale, line, line.Quantity * line.UnitFactor, line.LineTotal, line.LineTotal - line.VatAmount,
                SalePricing.Round(line.Batches.Sum(b => b.BaseQuantity * prices.GetValueOrDefault(b.BatchId)))));
        }

        foreach (var back in returns.SelectMany(r => r.Return.Lines))
        {
            var (sale, line) = lines[back.SaleLineId];
            entries.Add(new Entry(sale, line, -back.Quantity * line.UnitFactor, -back.Amount, -(back.Amount - back.VatAmount),
                -SalePricing.Round(back.Batches.Sum(b => b.BaseQuantity * prices.GetValueOrDefault(b.BatchId)))));
        }

        var products = (await catalog.GetProductsAsync([.. entries.Select(e => e.Line.ProductId).Distinct()], cancellationToken)).ToDictionary(p => p.ProductId);
        var groups = (await catalog.ListGroupsAsync(cancellationToken)).ToDictionary(g => g.Id, g => g.Name);
        var names = await customers.GetNamesAsync([.. entries.Where(e => e.Sale.CustomerId is not null).Select(e => e.Sale.CustomerId!.Value).Distinct()], cancellationToken);

        var rows = entries
            .GroupBy(e => view switch
            {
                StatisticsView.Groups => products.GetValueOrDefault(e.Line.ProductId)?.ProductGroupId?.ToString() ?? "-",
                StatisticsView.Users => e.Sale.UserId?.ToString() ?? "-",
                StatisticsView.Customers => e.Sale.CustomerId?.ToString() ?? "-",
                _ => e.Line.ProductId.ToString(),
            })
            .Select(g =>
            {
                var first = g.First();
                var product = products.GetValueOrDefault(first.Line.ProductId);
                var (key, name) = view switch
                {
                    StatisticsView.Groups => (g.Key, Guid.TryParse(g.Key, out var group) ? groups.GetValueOrDefault(group, "?") : "Без група"),
                    StatisticsView.Users => (g.Key, g.Key),
                    StatisticsView.Customers => (g.Key, Guid.TryParse(g.Key, out var customer) ? names.GetValueOrDefault(customer, "?") : "Без клиент"),
                    _ => (g.Key, product?.Name ?? first.Line.ProductName),
                };
                return new StatisticsRow(
                    key,
                    name,
                    view == StatisticsView.Products ? product?.Code : null,
                    view == StatisticsView.Products ? g.Sum(e => e.BaseQuantity) : null,
                    view == StatisticsView.Products ? product?.Units ?? [] : [],
                    g.Select(e => e.Sale.Id).Distinct().Count(),
                    g.Sum(e => e.Total),
                    g.Sum(e => e.Net),
                    g.Sum(e => e.Cost));
            })
            .OrderByDescending(r => r.Total)
            .ToList();

        return view == StatisticsView.Products ? WithAbc(rows) : rows;
    }

    /// <summary>A up to 80 % of the cumulative revenue, B up to 95 %, C the rest (rows already by revenue, largest first).</summary>
    private static List<StatisticsRow> WithAbc(List<StatisticsRow> rows)
    {
        var total = rows.Where(r => r.Total > 0).Sum(r => r.Total);
        var running = 0m;
        return rows.Select(r =>
        {
            var share = total == 0 ? 1m : running / total;
            running += Math.Max(0, r.Total);
            return r with { AbcClass = share < 0.80m ? 'A' : share < 0.95m ? 'B' : 'C' };
        }).ToList();
    }

    private sealed record Entry(Sale Sale, SaleLine Line, int BaseQuantity, decimal Total, decimal Net, decimal Cost);
}

/// <param name="Key">Product, group, user or customer id (as text); „-“ for none.</param>
/// <param name="BaseQuantity">Only by product: sold less returned, in base units.</param>
/// <param name="SaleCount">Sales (receipts) the row appears in.</param>
/// <param name="Total">Revenue with VAT.</param>
/// <param name="Net">Revenue without VAT.</param>
/// <param name="Cost">Purchase cost of the batches, without VAT.</param>
public sealed record StatisticsRow(
    string Key,
    string Name,
    long? Code,
    int? BaseQuantity,
    IReadOnlyList<CatalogUnit> Units,
    int SaleCount,
    decimal Total,
    decimal Net,
    decimal Cost,
    char? AbcClass = null)
{
    public decimal Profit => Net - Cost;

    /// <summary>Надценка: profit on cost, %.</summary>
    public decimal? MarkupPercent => Cost == 0 ? null : decimal.Round(Profit / Cost * 100m, 1, MidpointRounding.AwayFromZero);

    /// <summary>Марж: profit on revenue without VAT, %. A 25 % markup is a 20 % margin.</summary>
    public decimal? MarginPercent => Net == 0 ? null : decimal.Round(Profit / Net * 100m, 1, MidpointRounding.AwayFromZero);
}

using Apteka.Sales.Application.Common;
using Apteka.Sales.Domain.Returns;
using Apteka.Sales.Domain.Reversals;
using Apteka.Sales.Domain.Sales;

namespace Apteka.Web.Tests.Features.Sales;

internal sealed class FakeSales : ISaleRepository
{
    public List<Sale> Saved { get; } = [];

    public List<SaleReturn> Returns { get; } = [];

    public List<Reversal> Reversals { get; } = [];

    public Task<Sale?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Saved.SingleOrDefault(s => s.Id == id));

    public Task<IReadOnlyList<Sale>> ListAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Sale>>(Saved);

    public Task<IReadOnlyList<Sale>> ListForCustomerAsync(Guid customerId, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Sale>>(Saved.Where(s => s.CustomerId == customerId).ToList());

    public void Add(Sale sale) => Saved.Add(sale);

    public void Add(Reversal reversal) => Reversals.Add(reversal);

    public void Add(SaleReturn saleReturn) => Returns.Add(saleReturn);

    public Task<IReadOnlyList<SaleReturn>> ListReturnsAsync(Guid saleId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SaleReturn>>(Returns.Where(r => r.SaleId == saleId).ToList());

    public Task<IReadOnlyList<ReturnOfSale>> ListReturnsAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReturnOfSale>>(Returns
            .Select(r => Saved.Single(s => s.Id == r.SaleId) is var sale ? new ReturnOfSale(r, sale.Number, sale.PaymentMethod) : null!)
            .ToList());

    public Task<IReadOnlyList<BatchSale>> ListSalesOfBatchAsync(Guid batchId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<BatchSale>>(Saved
            .SelectMany(s => s.Lines.SelectMany(l => l.Batches.Where(b => b.BatchId == batchId)
                .Select(b => new BatchSale(s.Id, s.Number, s.CreatedAt, s.Status, s.CustomerId, l.ProductName, b.BaseQuantity, 0))))
            .ToList());

    public Task<Sale?> FindByNumberAsync(long number, CancellationToken cancellationToken) => Task.FromResult(Saved.SingleOrDefault(s => s.Number == number));

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

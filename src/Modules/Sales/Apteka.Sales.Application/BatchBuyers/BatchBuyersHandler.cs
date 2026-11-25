using Apteka.Sales.Application.Common;
using Apteka.Sales.Application.Customers;
using Apteka.Sales.Domain.Sales;

namespace Apteka.Sales.Application.BatchBuyers;

/// <summary>
/// „Кой е купил партидата“: for a recall, every sale that took goods from the batch, with the customer when one was
/// chosen at the till (F2), so the pharmacy can contact them. Reversed sales and what came back are shown as such.
/// </summary>
public sealed class BatchBuyersHandler(ISaleRepository sales, ICustomerRepository customers)
{
    public async Task<IReadOnlyList<BatchBuyer>> HandleAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var list = await sales.ListSalesOfBatchAsync(batchId, cancellationToken);
        var ids = list.Where(s => s.CustomerId is not null).Select(s => s.CustomerId!.Value).Distinct().ToList();
        var people = new Dictionary<Guid, (string Name, string? Phone)>();
        foreach (var id in ids)
        {
            if (await customers.GetAsync(id, cancellationToken) is { } customer)
            {
                people[id] = (customer.Name, customer.Phone);
            }
        }

        return list.Select(s =>
        {
            var person = s.CustomerId is { } c && people.TryGetValue(c, out var p) ? p : ((string Name, string? Phone)?)null;
            var kept = s.Status == SaleStatus.Reversed ? 0 : s.BaseQuantity - s.ReturnedBaseQuantity;
            return new BatchBuyer(s.SaleId, s.Number, s.CreatedAt, s.ProductName, s.BaseQuantity, kept, s.CustomerId, person?.Name, person?.Phone, s.Status == SaleStatus.Reversed);
        }).ToList();
    }
}

/// <param name="KeptBaseQuantity">Still with the customer: taken less reversed or returned.</param>
public sealed record BatchBuyer(
    Guid SaleId,
    long Number,
    DateTimeOffset CreatedAt,
    string ProductName,
    int BaseQuantity,
    int KeptBaseQuantity,
    Guid? CustomerId,
    string? CustomerName,
    string? CustomerPhone,
    bool Reversed);

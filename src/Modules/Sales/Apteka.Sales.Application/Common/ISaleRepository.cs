using Apteka.Sales.Domain.Reversals;
using Apteka.Sales.Domain.Sales;

namespace Apteka.Sales.Application.Common;

public interface ISaleRepository
{
    /// <summary>The sale with its lines and their batches, tracked for changes.</summary>
    Task<Sale?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Sale>> ListAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);

    /// <summary>The customer's sales, newest first, without lines.</summary>
    Task<IReadOnlyList<Sale>> ListForCustomerAsync(Guid customerId, int limit, CancellationToken cancellationToken);

    void Add(Sale sale);

    void Add(Reversal reversal);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

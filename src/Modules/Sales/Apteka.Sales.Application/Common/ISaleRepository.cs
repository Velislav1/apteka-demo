using Apteka.Sales.Domain.Returns;
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

    void Add(SaleReturn saleReturn);

    /// <summary>The returns of one sale, oldest first, with lines and batches.</summary>
    Task<IReadOnlyList<SaleReturn>> ListReturnsAsync(Guid saleId, CancellationToken cancellationToken);

    /// <summary>Returns made in [fromUtc, toUtc), with the number and payment of their sale.</summary>
    Task<IReadOnlyList<ReturnOfSale>> ListReturnsAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);

    /// <summary>Sales that took goods from this batch, with what each took and what came back with corrections.</summary>
    Task<IReadOnlyList<BatchSale>> ListSalesOfBatchAsync(Guid batchId, CancellationToken cancellationToken);

    /// <summary>The sale with this number, or null.</summary>
    Task<Sale?> FindByNumberAsync(long number, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record ReturnOfSale(SaleReturn Return, long SaleNumber, PaymentMethod PaymentMethod);

/// <param name="BaseQuantity">Taken from the batch, in base units.</param>
/// <param name="ReturnedBaseQuantity">Put back into the batch with corrections of this sale.</param>
public sealed record BatchSale(Guid SaleId, long Number, DateTimeOffset CreatedAt, SaleStatus Status, Guid? CustomerId, string ProductName, int BaseQuantity, int ReturnedBaseQuantity);

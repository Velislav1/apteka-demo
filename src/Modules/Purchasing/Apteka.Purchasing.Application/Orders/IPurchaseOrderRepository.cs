using Apteka.Purchasing.Domain.Orders;

namespace Apteka.Purchasing.Application.Orders;

public interface IPurchaseOrderRepository
{
    void Add(PurchaseOrder order);

    /// <summary>With its lines, tracked for changes.</summary>
    Task<PurchaseOrder?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Newest first.</summary>
    Task<IReadOnlyList<PurchaseOrderListItem>> ListAsync(int limit, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record PurchaseOrderListItem(
    Guid Id,
    long Number,
    string SupplierName,
    PurchaseOrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt,
    int LineCount);

using Apteka.Purchasing.Domain.Deliveries;
using Apteka.SharedKernel;

namespace Apteka.Purchasing.Application.Deliveries;

public interface IDeliveryRepository
{
    /// <summary>The delivery with all its lines (also removed ones), tracked for changes.</summary>
    Task<Delivery?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> InvoiceExistsAsync(Guid supplierId, string invoiceNo, Guid exceptId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DeliveryListItem>> ListAsync(Guid? supplierId, DeliveryStatus? status, int limit, CancellationToken cancellationToken);

    void Add(Delivery delivery);

    /// <summary>Saves; an invoice number taken by a concurrent save is reported as an error, not an exception.</summary>
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record DeliveryListItem(
    Guid Id,
    long Number,
    string SupplierName,
    string InvoiceNo,
    DateOnly InvoiceDate,
    DateTimeOffset ReceivedAt,
    DeliveryStatus Status,
    int LineCount,
    decimal PurchaseTotal);

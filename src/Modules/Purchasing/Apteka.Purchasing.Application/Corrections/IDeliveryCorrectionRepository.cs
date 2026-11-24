using Apteka.Purchasing.Domain.Corrections;
using Apteka.SharedKernel;

namespace Apteka.Purchasing.Application.Corrections;

public interface IDeliveryCorrectionRepository
{
    void Add(DeliveryCorrection correction);

    Task<DeliveryCorrection?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>All corrections of a delivery, oldest first, with their lines.</summary>
    Task<IReadOnlyList<DeliveryCorrection>> ListForDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken);

    /// <summary>Newest first, created in [fromUtc, toUtc).</summary>
    Task<IReadOnlyList<CorrectionListItem>> ListAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);

    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record CorrectionListItem(
    Guid Id,
    long Number,
    DateTimeOffset CreatedAt,
    Guid DeliveryId,
    long DeliveryNumber,
    string SupplierName,
    string InvoiceNo,
    string? NoteNo,
    DateOnly NoteDate,
    decimal PurchaseTotal);

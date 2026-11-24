namespace Apteka.Purchasing.Application.Corrections;

/// <param name="NoteNo">Number of the supplier's credit/debit note.</param>
public sealed record CorrectionInput(
    Guid DeliveryId,
    string? NoteNo,
    DateOnly NoteDate,
    string? Reason,
    IReadOnlyList<CorrectionLineInput> Lines);

/// <param name="QuantityDelta">+ or −, in the delivery line's unit; 0 lines are ignored.</param>
public sealed record CorrectionLineInput(Guid DeliveryLineId, int QuantityDelta);

public sealed record CorrectionView(
    Guid Id,
    long Number,
    DateTimeOffset CreatedAt,
    Guid DeliveryId,
    long DeliveryNumber,
    string InvoiceNo,
    string? NoteNo,
    DateOnly NoteDate,
    string? Reason,
    IReadOnlyList<CorrectionLineView> Lines,
    decimal PurchaseTotal);

public sealed record CorrectionLineView(
    int DeliveryLinePosition,
    string ProductName,
    string UnitName,
    string BatchNo,
    int QuantityDelta,
    decimal UnitPurchasePrice,
    decimal PurchaseTotal);

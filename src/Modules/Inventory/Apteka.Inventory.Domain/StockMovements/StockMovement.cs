using Apteka.Inventory.Domain.Batches;
using Apteka.SharedKernel;

namespace Apteka.Inventory.Domain.StockMovements;

/// <summary>
/// One change of a batch's quantity, in base units (+ in, − out). Append-only: never updated or deleted (rule 3);
/// the database rejects UPDATE and DELETE. A mistake is fixed with a new, opposite movement.
/// </summary>
public sealed class StockMovement : Entity<Guid>
{
    private StockMovement()
    {
        DocumentType = string.Empty;
    }

    private StockMovement(Guid id)
        : base(id)
    {
        DocumentType = string.Empty;
    }

    public Guid BatchId { get; private set; }

    public int QuantityDelta { get; private set; }

    public MovementKind Kind { get; private set; }

    public string DocumentType { get; private set; }

    public Guid DocumentId { get; private set; }

    public Guid? UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Stock received with a document (delivery): a positive quantity in base units.</summary>
    public static Result<StockMovement> Receipt(
        Batch batch,
        int baseQuantity,
        string documentType,
        Guid documentId,
        Guid? userId,
        DateTimeOffset at)
    {
        if (baseQuantity <= 0)
        {
            return Result.Failure<StockMovement>(InventoryErrors.QuantityMustBePositive);
        }

        return Result.Success(new StockMovement(Guid.CreateVersion7())
        {
            BatchId = batch.Id,
            QuantityDelta = baseQuantity,
            Kind = MovementKind.Delivery,
            DocumentType = documentType,
            DocumentId = documentId,
            UserId = userId,
            CreatedAt = at,
        });
    }

    /// <summary>Stock issued by a document (sale): a negative quantity in base units.</summary>
    public static Result<StockMovement> Issue(
        Guid batchId,
        int baseQuantity,
        string documentType,
        Guid documentId,
        Guid? userId,
        DateTimeOffset at) =>
        Create(batchId, -baseQuantity, baseQuantity, MovementKind.Sale, documentType, documentId, userId, at);

    /// <summary>Stock returned by a reversal: a positive quantity in base units, into the same batch.</summary>
    public static Result<StockMovement> Return(
        Guid batchId,
        int baseQuantity,
        string documentType,
        Guid documentId,
        Guid? userId,
        DateTimeOffset at) =>
        Create(batchId, baseQuantity, baseQuantity, MovementKind.Reversal, documentType, documentId, userId, at);

    private static Result<StockMovement> Create(
        Guid batchId,
        int delta,
        int baseQuantity,
        MovementKind kind,
        string documentType,
        Guid documentId,
        Guid? userId,
        DateTimeOffset at)
    {
        if (baseQuantity <= 0)
        {
            return Result.Failure<StockMovement>(InventoryErrors.QuantityMustBePositive);
        }

        return Result.Success(new StockMovement(Guid.CreateVersion7())
        {
            BatchId = batchId,
            QuantityDelta = delta,
            Kind = kind,
            DocumentType = documentType,
            DocumentId = documentId,
            UserId = userId,
            CreatedAt = at,
        });
    }
}

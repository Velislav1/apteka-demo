namespace Apteka.Inventory.Application.WriteOffs;

/// <summary>What the „Бракуване“ screen sends when the document is completed.</summary>
public sealed record WriteOffInput(string? Reason, IReadOnlyList<WriteOffLineInput> Lines, Domain.WriteOffs.WriteOffKind Kind = Domain.WriteOffs.WriteOffKind.WriteOff, string? Recipient = null);

/// <param name="Quantity">In the unit <paramref name="UnitId"/>.</param>
public sealed record WriteOffLineInput(Guid ProductId, Guid UnitId, int Quantity, Guid BatchId);

public sealed record WriteOffView(
    Guid Id,
    long Number,
    DateTimeOffset CreatedAt,
    Guid? UserId,
    string? Reason,
    IReadOnlyList<WriteOffLineView> Lines,
    decimal PurchaseTotal,
    Domain.WriteOffs.WriteOffKind Kind = Domain.WriteOffs.WriteOffKind.WriteOff,
    string? Recipient = null);

public sealed record WriteOffLineView(
    int Position,
    Guid ProductId,
    string ProductName,
    string UnitName,
    int Quantity,
    string BatchNo,
    DateOnly ExpiryDate,
    decimal PurchaseTotal);

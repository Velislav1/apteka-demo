using Apteka.SharedKernel;

namespace Apteka.Inventory.Contracts;

/// <summary>
/// Takes goods out of stock (sale) and puts them back (reversal). Must run inside the caller's
/// <see cref="IUnitOfWork"/>: the batches of each product are locked until the transaction ends (rule 4),
/// so two tills can never sell the same last box.
/// </summary>
public interface IStockIssuing
{
    /// <summary>Issues every line from the batches chosen by FEFO; fails as a whole if any line has too little stock.</summary>
    Task<Result<IReadOnlyList<IssuedLine>>> IssueAsync(StockIssue issue, CancellationToken cancellationToken = default);

    /// <summary>Puts back exactly the given quantities into the given batches.</summary>
    Task<Result> ReturnAsync(StockReturn stockReturn, CancellationToken cancellationToken = default);
}

/// <param name="Today">The Sofia date: batches that expired before it are not issued.</param>
public sealed record StockIssue(string DocumentType, Guid DocumentId, Guid? UserId, DateOnly Today, IReadOnlyList<StockIssueLine> Lines);

/// <param name="LineId">The caller's id for the line; returned with its batches.</param>
public sealed record StockIssueLine(Guid LineId, Guid ProductId, int BaseQuantity);

/// <summary>One line, split over one or more batches, with each batch's prices.</summary>
public sealed record IssuedLine(Guid LineId, Guid ProductId, IReadOnlyList<IssuedBatch> Batches);

public sealed record IssuedBatch(
    Guid BatchId,
    string BatchNo,
    DateOnly ExpiryDate,
    int BaseQuantity,
    decimal SalePricePerBaseUnit,
    int PricedUnitFactor,
    decimal PricedUnitSalePrice);

public sealed record StockReturn(string DocumentType, Guid DocumentId, Guid? UserId, IReadOnlyList<ReturnedBatch> Batches);

public sealed record ReturnedBatch(Guid BatchId, int BaseQuantity);

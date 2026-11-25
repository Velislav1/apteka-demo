using Apteka.SharedKernel;

namespace Apteka.Inventory.Domain.BatchBlocks;

/// <summary>
/// One step in the blocking history of a batch: blocked (e.g. a recall signal from the Bulgarian Drug Agency, a
/// damaged delivery) or released again. The latest step is the batch's state. A blocked batch is never sold (FEFO
/// skips it); it can still be written off, returned to the supplier or counted. The history only grows.
/// </summary>
public sealed class BatchBlock : Entity<Guid>
{
    public const int MaxReasonLength = 200;

    private BatchBlock()
    {
    }

    private BatchBlock(Guid id)
        : base(id)
    {
    }

    public Guid BatchId { get; private set; }

    public bool IsBlocked { get; private set; }

    public string? Reason { get; private set; }

    public Guid? UserId { get; private set; }

    public DateTimeOffset At { get; private set; }

    /// <param name="currentlyBlocked">The batch's state now (the latest step), so that a step always changes it.</param>
    public static Result<BatchBlock> Create(Guid batchId, bool block, bool currentlyBlocked, string? reason, Guid? userId, DateTimeOffset at)
    {
        if (block == currentlyBlocked)
        {
            return Result.Failure<BatchBlock>(block ? BatchBlockErrors.AlreadyBlocked : BatchBlockErrors.NotBlocked);
        }

        var trimmed = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (block && trimmed is null)
        {
            return Result.Failure<BatchBlock>(BatchBlockErrors.ReasonRequired);
        }

        if (trimmed is { Length: > MaxReasonLength })
        {
            return Result.Failure<BatchBlock>(BatchBlockErrors.ReasonTooLong);
        }

        return Result.Success(new BatchBlock(Guid.CreateVersion7()) { BatchId = batchId, IsBlocked = block, Reason = trimmed, UserId = userId, At = at });
    }
}

/// <summary>Errors of blocking a batch, shown to the user in Bulgarian.</summary>
public static class BatchBlockErrors
{
    public static readonly Error ReasonRequired = new("inventory.block_reason_required", "Въведете причина за блокирането (напр. писмо на ИАЛ № …).");
    public static readonly Error ReasonTooLong = new("inventory.block_reason_too_long", $"Причината е по-дълга от {BatchBlock.MaxReasonLength} знака.");
    public static readonly Error AlreadyBlocked = new("inventory.already_blocked", "Партидата вече е блокирана.");
    public static readonly Error NotBlocked = new("inventory.not_blocked", "Партидата не е блокирана.");
    public static readonly Error BatchNotFound = new("inventory.block_batch_not_found", "Партидата не е намерена.");
}

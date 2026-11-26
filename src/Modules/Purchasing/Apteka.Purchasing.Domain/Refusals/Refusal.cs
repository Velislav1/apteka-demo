using Apteka.SharedKernel;

namespace Apteka.Purchasing.Domain.Refusals;

/// <summary>
/// „Отказ“ (Apteka Expert 2): a customer asked for goods the pharmacy did not have. Kept so the demand is not lost:
/// the next purchase order can add it, and the report shows what was missing most often. Never changed or deleted.
/// </summary>
public sealed class Refusal : Entity<Guid>
{
    public const int MaxTextLength = 200;

    private Refusal()
    {
    }

    private Refusal(Guid id)
        : base(id)
    {
    }

    /// <summary>The product, when it is in the catalog.</summary>
    public Guid? ProductId { get; private set; }

    /// <summary>What was asked for, when it is not in the catalog (e.g. a product the pharmacy never had).</summary>
    public string? Text { get; private set; }

    /// <summary>In base units of the product (or pieces for free text).</summary>
    public int Quantity { get; private set; }

    public string? Note { get; private set; }

    public Guid? UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Refusal> Create(Guid? productId, string? text, int quantity, string? note, Guid? userId, DateTimeOffset at)
    {
        var what = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        var why = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (productId is null && what is null)
        {
            return Result.Failure<Refusal>(RefusalErrors.WhatRequired);
        }

        if (quantity <= 0)
        {
            return Result.Failure<Refusal>(RefusalErrors.QuantityMustBePositive);
        }

        if (what is { Length: > MaxTextLength } || why is { Length: > MaxTextLength })
        {
            return Result.Failure<Refusal>(RefusalErrors.TooLong);
        }

        return Result.Success(new Refusal(Guid.CreateVersion7()) { ProductId = productId, Text = what, Quantity = quantity, Note = why, UserId = userId, CreatedAt = at });
    }
}

public static class RefusalErrors
{
    public static readonly Error WhatRequired = new("purchasing.refusal_what", "Изберете стоката или напишете какво е търсено.");
    public static readonly Error QuantityMustBePositive = new("purchasing.refusal_quantity", "Количеството трябва да е по-голямо от нула.");
    public static readonly Error TooLong = new("purchasing.refusal_too_long", $"Текстът е по-дълъг от {Refusal.MaxTextLength} знака.");
}

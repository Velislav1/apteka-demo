using Apteka.SharedKernel;

namespace Apteka.Purchasing.Domain.Expenses;

/// <summary>
/// „Въвеждане на разходи“ (Apteka Expert 2): costs of the pharmacy other than goods — rent, electricity, salaries …
/// They go into „Рекапитулация“. Never changed or deleted: a mistake is fixed with an entry of the opposite sign.
/// </summary>
public sealed class Expense : Entity<Guid>
{
    public const int MaxTextLength = 200;

    /// <summary>The usual categories; any other text is allowed too.</summary>
    public static readonly IReadOnlyList<string> Categories = ["Наем", "Ток, вода, отопление", "Заплати и осигуровки", "Счетоводство", "Банкови такси", "Комуникации", "Консумативи", "Ремонт и поддръжка", "Данъци и такси", "Други"];

    private Expense()
    {
        Category = string.Empty;
    }

    private Expense(Guid id)
        : base(id)
    {
        Category = string.Empty;
    }

    public DateOnly Date { get; private set; }

    public string Category { get; private set; }

    /// <summary>With VAT, in euro; negative corrects an earlier entry.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Invoice or receipt number.</summary>
    public string? DocumentNo { get; private set; }

    public string? Note { get; private set; }

    public Guid? UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Expense> Create(DateOnly date, string category, decimal amount, string? documentNo, string? note, Guid? userId, DateTimeOffset at)
    {
        var what = category.Trim();
        var doc = string.IsNullOrWhiteSpace(documentNo) ? null : documentNo.Trim();
        var why = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (what.Length == 0)
        {
            return Result.Failure<Expense>(ExpenseErrors.CategoryRequired);
        }

        if (amount == 0)
        {
            return Result.Failure<Expense>(ExpenseErrors.AmountRequired);
        }

        if (what.Length > MaxTextLength || doc is { Length: > 30 } || why is { Length: > MaxTextLength })
        {
            return Result.Failure<Expense>(ExpenseErrors.TooLong);
        }

        return Result.Success(new Expense(Guid.CreateVersion7()) { Date = date, Category = what, Amount = amount, DocumentNo = doc, Note = why, UserId = userId, CreatedAt = at });
    }
}

public static class ExpenseErrors
{
    public static readonly Error CategoryRequired = new("purchasing.expense_category", "Изберете или напишете вид разход.");
    public static readonly Error AmountRequired = new("purchasing.expense_amount", "Въведете сума (отрицателна — за поправка на грешка).");
    public static readonly Error TooLong = new("purchasing.expense_too_long", "Текстът е твърде дълъг (вид и бележка до 200 знака, документ до 30).");
}

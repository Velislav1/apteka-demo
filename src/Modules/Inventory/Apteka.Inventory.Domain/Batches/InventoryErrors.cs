using Apteka.SharedKernel;

namespace Apteka.Inventory.Domain.Batches;

/// <summary>Validation errors of the inventory. Messages are shown to the user, so they are in Bulgarian.</summary>
public static class InventoryErrors
{
    public static readonly Error BatchNoRequired = new("inventory.batch_no_required", "Въведете номер на партида.");
    public static readonly Error BatchNoTooLong = new("inventory.batch_no_too_long", $"Номерът на партида е по-дълъг от {Batch.MaxBatchNoLength} знака.");
    public static readonly Error InvalidFactor = new("inventory.invalid_factor", "Невалиден коефициент на мярка.");
    public static readonly Error InvalidPrice = new("inventory.invalid_price", "Покупната цена не може да е отрицателна, а продажната трябва да е по-голяма от нула.");
    public static readonly Error InvalidStockType = new("inventory.invalid_stock_type", "Невалиден склад.");
    public static readonly Error QuantityMustBePositive = new("inventory.quantity_positive", "Количеството трябва да е по-голямо от нула.");
    public static readonly Error ReceiptEmpty = new("inventory.receipt_empty", "Няма редове за заприхождаване.");
    public static readonly Error IssueEmpty = new("inventory.issue_empty", "Няма редове за изписване.");
    public static readonly Error NothingToReturn = new("inventory.nothing_to_return", "Няма какво да се върне.");
    public static readonly Error UnknownBatch = new("inventory.unknown_batch", "Партидата не е намерена.");

    public static Error InsufficientStock(int available) =>
        new("inventory.insufficient_stock", $"Няма достатъчна наличност (налични в най-малка мярка, без изтекли партиди: {available}).");

    public static readonly Error CorrectionEmpty = new("inventory.correction_empty", "Няма редове за корекция.");

    public static Error NotReceived(int line) =>
        new("inventory.not_received", $"Ред {line}: редът не е заприходен.");

    public static Error CorrectionBelowZero(int line, string batchNo, int available) =>
        new("inventory.correction_below_zero", $"Ред {line}: в партида {batchNo} са останали само {available} (в най-малка мярка) — не може да се намали повече.");

    public static readonly Error AlreadyReceived = new("inventory.already_received", "Тези редове вече са заприходени.");
}

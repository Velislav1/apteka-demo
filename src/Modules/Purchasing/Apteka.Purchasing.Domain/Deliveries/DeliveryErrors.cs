using Apteka.SharedKernel;

namespace Apteka.Purchasing.Domain.Deliveries;

/// <summary>Validation errors of deliveries. Messages are shown to the user, so they are in Bulgarian.</summary>
public static class DeliveryErrors
{
    public const int MaxInvoiceNoLength = 30;
    public const int MaxBatchNoLength = 30;

    public static readonly Error InvoiceNoRequired = new("purchasing.invoice_no_required", "Въведете номер на фактурата.");
    public static readonly Error InvoiceNoTooLong = new("purchasing.invoice_no_too_long", $"Номерът на фактурата е по-дълъг от {MaxInvoiceNoLength} знака.");
    public static readonly Error NotDraft = new("purchasing.not_draft", "Публикувана доставка не може да се променя.");
    public static readonly Error NoLines = new("purchasing.no_lines", "Добавете поне един ред.");
    public static readonly Error UnknownLine = new("purchasing.unknown_line", "Редът не принадлежи на тази доставка.");
    public static readonly Error NotFound = new("purchasing.delivery_not_found", "Доставката не е намерена.");

    public static Error InvoiceTaken(string invoiceNo) =>
        new("purchasing.invoice_taken", $"Фактура {invoiceNo} от този доставчик вече е въведена.");

    public static Error QuantityMustBePositive(int line) =>
        new("purchasing.quantity_positive", $"Ред {line}: количеството трябва да е цяло число, по-голямо от нула.");

    public static Error InvalidPurchasePrice(int line) =>
        new("purchasing.invalid_purchase_price", $"Ред {line}: покупната цена не може да е отрицателна.");

    public static Error InvalidSalePrice(int line) =>
        new("purchasing.invalid_sale_price", $"Ред {line}: продажната цена трябва да е по-голяма от нула.");

    public static Error InvalidMarkup(int line) =>
        new("purchasing.invalid_markup", $"Ред {line}: надценката не може да е отрицателна.");

    public static Error BatchNoRequired(int line) =>
        new("purchasing.batch_no_required", $"Ред {line}: въведете номер на партида (до {MaxBatchNoLength} знака).");

    public static Error InvalidUnit(int line) =>
        new("purchasing.invalid_unit", $"Ред {line}: мярката не е от тази стока.");

    public static Error ProductNotAvailable(int line) =>
        new("purchasing.product_not_available", $"Ред {line}: стоката не съществува или е неактивна.");

    public static Error Expired(int line, DateOnly expiry) =>
        new("purchasing.expired", $"Ред {line}: срокът на годност ({expiry:dd.MM.yyyy}) е изтекъл.");
}

using Apteka.SharedKernel;

namespace Apteka.Purchasing.Domain.Orders;

/// <summary>Errors of „Заявка за доставка“, shown to the user in Bulgarian.</summary>
public static class OrderErrors
{
    public static readonly Error NotFound = new("purchasing.order_not_found", "Заявката не е намерена.");
    public static readonly Error NotDraft = new("purchasing.order_not_draft", "Заявката е изпратена и не може да се променя.");
    public static readonly Error NoLines = new("purchasing.order_no_lines", "Заявката няма стоки с количество.");
    public static readonly Error NoteTooLong = new("purchasing.order_note_too_long", $"Бележката е по-дълга от {PurchaseOrder.MaxNoteLength} знака.");
    public static readonly Error DuplicateProduct = new("purchasing.order_duplicate", "Една стока е въведена два пъти в една и съща мярка.");
    public static readonly Error SupplierNotAvailable = new("purchasing.order_supplier", "Изберете активен доставчик.");
    public static readonly Error InvalidDays = new("purchasing.order_days", "Дните трябва да са от 1 до 365.");

    public static Error InvalidQuantity(int line) => new("purchasing.order_quantity", $"Ред {line}: количеството трябва да е цяло положително число.");
}

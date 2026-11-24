using Apteka.SharedKernel;

namespace Apteka.Sales.Domain.Customers;

/// <summary>Errors of „Клиенти“, shown to the user in Bulgarian.</summary>
public static class CustomerErrors
{
    public static readonly Error NameRequired = new("sales.customer_name_required", "Въведете име на клиента.");
    public static readonly Error TooLong = new("sales.customer_too_long", $"Името е до {Customer.MaxNameLength} знака, бележката — до {Customer.MaxNoteLength}.");
    public static readonly Error InvalidEgn = new("sales.customer_invalid_egn", "Невалидно ЕГН: очакват се 10 цифри с вярна дата и контролна цифра.");
    public static readonly Error InvalidPhone = new("sales.customer_invalid_phone", "Телефонът може да съдържа само цифри, интервал, +, - и / (до 30 знака).");
    public static readonly Error EgnTaken = new("sales.customer_egn_taken", "Има друг клиент с това ЕГН.");
    public static readonly Error NotFound = new("sales.customer_not_found", "Клиентът не е намерен.");
    public static readonly Error NotAvailable = new("sales.customer_not_available", "Клиентът не съществува или е неактивен.");
}

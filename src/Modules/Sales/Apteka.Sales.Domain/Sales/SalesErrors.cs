using Apteka.SharedKernel;

namespace Apteka.Sales.Domain.Sales;

/// <summary>Errors of the till. Messages are shown to the user, so they are in Bulgarian.</summary>
public static class SalesErrors
{
    public static readonly Error NoLines = new("sales.no_lines", "Няма стоки за продажба.");
    public static readonly Error CashTooLow = new("sales.cash_too_low", "Получената сума е по-малка от дължимата.");
    public static readonly Error InvalidPayment = new("sales.invalid_payment", "Невалиден начин на плащане.");
    public static readonly Error OnAccountNeedsCustomer = new("sales.on_account_customer", "Продажба на отложено плащане е само на клиент: изберете клиента с F2.");
    public static readonly Error InvalidPaymentTerm = new("sales.payment_term", "Дните за плащане трябва да са от 1 до 365.");
    public static readonly Error AlreadyReversed = new("sales.already_reversed", "Продажбата вече е сторнирана.");
    public static readonly Error NotFound = new("sales.not_found", "Продажбата не е намерена.");
    public static readonly Error ManagersOnly = new("sales.managers_only", "Само управител може да сторнира продажба.");
    public static readonly Error ReasonTooLong = new("sales.reason_too_long", "Причината е по-дълга от 200 знака.");

    public static Error QuantityMustBePositive(string product) =>
        new("sales.quantity_positive", $"„{product}“: количеството трябва да е цяло число, по-голямо от нула.");

    public static Error InvalidDiscount(string product) =>
        new("sales.invalid_discount", $"„{product}“: отстъпката трябва да е от 0 до 100 %.");

    public static Error InvalidPrice(string product) =>
        new("sales.invalid_price", $"„{product}“: няма продажна цена.");

    public static Error ProductNotAvailable(string product) =>
        new("sales.product_not_available", $"„{product}“ не съществува или е неактивна.");

    public static Error NarcoticNotAllowed(string product) =>
        new("sales.narcotic_not_allowed", $"„{product}“ е упойващо вещество и не се продава на свободна продажба.");

    public static Error PrescriptionNotConfirmed(string product) =>
        new("sales.prescription_not_confirmed", $"„{product}“ се отпуска по лекарско предписание: потвърдете рецептата.");

    public static Error NotEnoughStock(string product, string detail) =>
        new("sales.not_enough_stock", $"„{product}“: {detail}");
}

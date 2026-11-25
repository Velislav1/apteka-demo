using Apteka.SharedKernel;

namespace Apteka.Purchasing.Domain.Payments;

/// <summary>Errors of „Плащания към доставчици“, shown to the user in Bulgarian.</summary>
public static class PaymentErrors
{
    public static readonly Error AmountMustBePositive = new("purchasing.payment_amount", "Сумата трябва да е по-голяма от нула.");
    public static readonly Error NoteTooLong = new("purchasing.payment_note", $"Бележката е по-дълга от {SupplierPayment.MaxNoteLength} знака.");
    public static readonly Error SupplierNotFound = new("purchasing.payment_supplier", "Доставчикът не е намерен.");

    public static Error MoreThanOwed(decimal owed) =>
        new("purchasing.payment_too_much", $"Сумата е по-голяма от задълженията към доставчика ({owed:0.00} €).");
}

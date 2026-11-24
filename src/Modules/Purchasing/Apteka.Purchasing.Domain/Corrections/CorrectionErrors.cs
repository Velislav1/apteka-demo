using Apteka.SharedKernel;

namespace Apteka.Purchasing.Domain.Corrections;

/// <summary>Errors of „Корекционна складова разписка“, shown to the user in Bulgarian.</summary>
public static class CorrectionErrors
{
    public static readonly Error DeliveryNotFound = new("purchasing.correction_delivery_not_found", "Доставката не е намерена.");
    public static readonly Error DeliveryNotPosted = new("purchasing.correction_not_posted", "Коригира се само приключена доставка; черновата се поправя направо.");
    public static readonly Error NoLines = new("purchasing.correction_no_lines", "Въведете поне една промяна (+ или −) по редовете.");
    public static readonly Error UnknownLine = new("purchasing.correction_unknown_line", "Редът не е от тази доставка.");
    public static readonly Error DuplicateLine = new("purchasing.correction_duplicate_line", "Един ред е въведен два пъти.");
    public static readonly Error TextTooLong = new("purchasing.correction_text_too_long", $"Номерът на известието е до {DeliveryCorrection.MaxNoteNoLength} знака, основанието — до {DeliveryCorrection.MaxReasonLength}.");

    public static Error BelowZero(int line) =>
        new("purchasing.correction_below_zero", $"Ред {line}: не може да се намали повече от полученото по доставката.");
}

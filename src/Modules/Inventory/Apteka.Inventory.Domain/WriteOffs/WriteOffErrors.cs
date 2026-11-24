using Apteka.SharedKernel;

namespace Apteka.Inventory.Domain.WriteOffs;

/// <summary>Errors of „Бракуване“, shown to the user in Bulgarian.</summary>
public static class WriteOffErrors
{
    public static readonly Error NoLines = new("inventory.write_off_no_lines", "Добавете поне една стока за бракуване.");
    public static readonly Error ReasonTooLong = new("inventory.write_off_reason_too_long", $"Основанието е по-дълго от {WriteOff.MaxReasonLength} знака.");

    public static Error QuantityMustBePositive(int line) =>
        new("inventory.write_off_quantity", $"Ред {line}: количеството трябва да е по-голямо от нула.");

    public static Error BatchNotOfProduct(int line) =>
        new("inventory.write_off_batch", $"Ред {line}: партидата не е на тази стока или не е в свободна продажба.");

    public static Error ProductNotFound(int line) =>
        new("inventory.write_off_product", $"Ред {line}: стоката или мярката не е намерена.");

    public static Error NotEnoughInBatch(int line, string batchNo, int available) =>
        new("inventory.write_off_insufficient", $"Ред {line}: в партида {batchNo} има само {available} (в най-малка мярка).");
}

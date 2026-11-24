using Apteka.SharedKernel;

namespace Apteka.Purchasing.Domain.Suppliers;

/// <summary>A wholesaler the pharmacy buys from. Deactivated instead of deleted.</summary>
public sealed class Supplier : Entity<Guid>
{
    public const int MaxNameLength = 200;
    public const int MaxPaymentTermDays = 365;

    private Supplier()
    {
        Name = string.Empty;
        Eik = string.Empty;
    }

    private Supplier(Guid id)
        : base(id)
    {
        Name = string.Empty;
        Eik = string.Empty;
    }

    public string Name { get; private set; }

    public string Eik { get; private set; }

    /// <summary>Days after the invoice date by which the supplier must be paid.</summary>
    public int PaymentTermDays { get; private set; }

    public bool IsActive { get; private set; }

    public static Result<Supplier> Create(string name, string eik, int paymentTermDays)
    {
        var supplier = new Supplier(Guid.CreateVersion7());
        var result = supplier.Update(name, eik, paymentTermDays, isActive: true);
        return result.IsSuccess ? Result.Success(supplier) : Result.Failure<Supplier>(result.Error);
    }

    public Result Update(string name, string eik, int paymentTermDays, bool isActive)
    {
        var trimmedName = name.Trim();
        var trimmedEik = eik.Trim();

        if (trimmedName.Length == 0)
        {
            return Result.Failure(SupplierErrors.NameRequired);
        }

        if (trimmedName.Length > MaxNameLength)
        {
            return Result.Failure(SupplierErrors.NameTooLong);
        }

        if (!Suppliers.Eik.IsValid(trimmedEik))
        {
            return Result.Failure(SupplierErrors.InvalidEik);
        }

        if (paymentTermDays is < 0 or > MaxPaymentTermDays)
        {
            return Result.Failure(SupplierErrors.InvalidPaymentTerm);
        }

        Name = trimmedName;
        Eik = trimmedEik;
        PaymentTermDays = paymentTermDays;
        IsActive = isActive;
        return Result.Success();
    }
}

public static class SupplierErrors
{
    public static readonly Error NameRequired = new("purchasing.supplier_name_required", "Въведете име на доставчика.");
    public static readonly Error NameTooLong = new("purchasing.supplier_name_too_long", $"Името е по-дълго от {Supplier.MaxNameLength} знака.");
    public static readonly Error InvalidEik = new("purchasing.invalid_eik", "Невалиден ЕИК: очакват се 9 или 13 цифри с вярна контролна цифра.");
    public static readonly Error InvalidPaymentTerm = new("purchasing.invalid_payment_term", $"Срокът за плащане трябва да е от 0 до {Supplier.MaxPaymentTermDays} дни.");
    public static readonly Error NotFound = new("purchasing.supplier_not_found", "Доставчикът не е намерен.");
    public static readonly Error NotAvailable = new("purchasing.supplier_not_available", "Доставчикът не съществува или е неактивен.");

    public static Error NameTaken(string name) => new("purchasing.supplier_name_taken", $"Доставчик „{name}“ вече съществува.");

    public static Error EikTaken(string eik) => new("purchasing.eik_taken", $"Доставчик с ЕИК {eik} вече съществува.");
}

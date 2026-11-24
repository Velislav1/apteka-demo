using Apteka.SharedKernel;

namespace Apteka.Sales.Domain.Customers;

/// <summary>
/// „Клиент / пациент“ as in Apteka Expert 2: who bought, so sales can be found by name, ЕГН, code or phone.
/// Never deleted, only deactivated (rule 6). <see cref="Code"/> comes from a database sequence.
/// </summary>
public sealed class Customer : Entity<Guid>
{
    public const int MaxNameLength = 200;
    public const int MaxPhoneLength = 30;
    public const int MaxNoteLength = 200;

    private Customer()
    {
        Name = string.Empty;
    }

    private Customer(Guid id)
        : base(id)
    {
        Name = string.Empty;
    }

    public long Code { get; private set; }

    public string Name { get; private set; }

    /// <summary>ЕГН; optional, unique when given.</summary>
    public string? Egn { get; private set; }

    public string? Phone { get; private set; }

    public string? Note { get; private set; }

    public bool IsActive { get; private set; }

    public static Result<Customer> Create(CustomerDetails details)
    {
        var customer = new Customer(Guid.CreateVersion7());
        var result = customer.Update(details);
        return result.IsFailure ? Result.Failure<Customer>(result.Error) : Result.Success(customer);
    }

    public Result Update(CustomerDetails details)
    {
        var name = details.Name.Trim();
        var egn = string.IsNullOrWhiteSpace(details.Egn) ? null : details.Egn.Trim();
        var phone = string.IsNullOrWhiteSpace(details.Phone) ? null : details.Phone.Trim();
        var note = string.IsNullOrWhiteSpace(details.Note) ? null : details.Note.Trim();

        if (name.Length == 0)
        {
            return Result.Failure(CustomerErrors.NameRequired);
        }

        if (name.Length > MaxNameLength || note is { Length: > MaxNoteLength })
        {
            return Result.Failure(CustomerErrors.TooLong);
        }

        if (egn is not null && !Customers.Egn.IsValid(egn))
        {
            return Result.Failure(CustomerErrors.InvalidEgn);
        }

        if (phone is not null && (phone.Length > MaxPhoneLength || !phone.All(c => char.IsAsciiDigit(c) || c is ' ' or '+' or '-' or '/')))
        {
            return Result.Failure(CustomerErrors.InvalidPhone);
        }

        Name = name;
        Egn = egn;
        Phone = phone;
        Note = note;
        IsActive = details.IsActive;
        return Result.Success();
    }
}

public sealed record CustomerDetails(string Name, string? Egn, string? Phone, string? Note, bool IsActive = true);

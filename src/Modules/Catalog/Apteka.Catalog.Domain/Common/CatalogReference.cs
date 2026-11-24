using Apteka.SharedKernel;

namespace Apteka.Catalog.Domain.Common;

/// <summary>
/// A simple named list item of the catalog (manufacturer, product group, shelf).
/// Never deleted: an item that is no longer used is deactivated.
/// </summary>
public abstract class CatalogReference : Entity<Guid>
{
    public const int MaxNameLength = 200;

    protected CatalogReference()
    {
        Name = string.Empty;
    }

    protected CatalogReference(Guid id)
        : base(id)
    {
        Name = string.Empty;
    }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    public Result Update(string name, bool isActive)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return Result.Failure(ReferenceErrors.NameRequired);
        }

        if (trimmed.Length > MaxNameLength)
        {
            return Result.Failure(ReferenceErrors.NameTooLong);
        }

        Name = trimmed;
        IsActive = isActive;
        return Result.Success();
    }

    protected static Result<T> Create<T>(T item, string name)
        where T : CatalogReference
    {
        var result = item.Update(name, isActive: true);
        return result.IsSuccess ? Result.Success(item) : Result.Failure<T>(result.Error);
    }
}

public static class ReferenceErrors
{
    public static readonly Error NameRequired = new("catalog.reference_name_required", "Въведете име.");

    public static readonly Error NameTooLong =
        new("catalog.reference_name_too_long", $"Името е по-дълго от {CatalogReference.MaxNameLength} знака.");

    public static readonly Error NotFound = new("catalog.reference_not_found", "Записът не е намерен.");

    public static Error NameTaken(string name) =>
        new("catalog.reference_name_taken", $"„{name}“ вече съществува.");
}

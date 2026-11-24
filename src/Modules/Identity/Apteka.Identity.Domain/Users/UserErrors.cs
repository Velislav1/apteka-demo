using Apteka.SharedKernel;

namespace Apteka.Identity.Domain.Users;

/// <summary>Errors of login and user management. Messages are shown to the user, so they are in Bulgarian.</summary>
public static class UserErrors
{
    /// <summary>The same message for a wrong name, a wrong password and an inactive user: it does not reveal which one.</summary>
    public static readonly Error InvalidCredentials = new("identity.invalid_credentials", "Грешно потребителско име или парола.");
    public static readonly Error InvalidUserName = new("identity.invalid_user_name", "Потребителското име трябва да е от 3 до 50 знака: латински малки букви, цифри, точка, тире или долна черта.");
    public static readonly Error DisplayNameRequired = new("identity.display_name_required", "Въведете име (до 100 знака).");
    public static readonly Error InvalidRole = new("identity.invalid_role", "Невалидна роля.");
    public static readonly Error WeakPassword = new("identity.weak_password", $"Паролата трябва да е поне {PasswordPolicy.MinLength} знака и да съдържа буква и цифра.");
    public static readonly Error NotFound = new("identity.user_not_found", "Потребителят не е намерен.");
    public static readonly Error LastManager = new("identity.last_manager", "Трябва да остане поне един активен управител.");
    public static readonly Error ManagersOnly = new("identity.managers_only", "Само управител може да управлява потребители.");

    public static Error LockedOut(DateTimeOffset until) =>
        new("identity.locked_out", $"Твърде много грешни опити. Опитайте отново след {BulgarianTime.ToLocal(until):HH:mm} ч.");

    public static Error UserNameTaken(string userName) =>
        new("identity.user_name_taken", $"Потребител „{userName}“ вече съществува.");
}

namespace Apteka.Identity.Domain.Users;

/// <summary>Minimum password rules; checked before the password is hashed (the hash is all that is stored).</summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public static bool IsStrongEnough(string password) =>
        password.Length is >= MinLength and <= MaxLength
        && password.Any(char.IsLetter)
        && password.Any(char.IsDigit);
}

using Apteka.Identity.Domain.Users;

namespace Apteka.Identity.Tests.Users;

public class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 24, 9, 0, 0, TimeSpan.Zero);

    private static User NewUser() => User.Create(" Maria.P ", " Мария П. ", UserRole.Pharmacist, "hash").Value;

    [Fact]
    public void User_name_is_lower_case_and_new_users_are_active()
    {
        var user = NewUser();

        Assert.Equal("maria.p", user.UserName);
        Assert.Equal("Мария П.", user.DisplayName);
        Assert.True(user.IsActive);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("мария")]
    [InlineData("maria p")]
    public void Invalid_user_names_are_rejected(string userName) =>
        Assert.Equal(UserErrors.InvalidUserName, User.Create(userName, "Мария", UserRole.Pharmacist, "hash").Error);

    [Theory]
    [InlineData("short1", false)]
    [InlineData("onlyletters", false)]
    [InlineData("12345678", false)]
    [InlineData("parola2026", true)]
    public void Password_needs_eight_characters_with_a_letter_and_a_digit(string password, bool ok) =>
        Assert.Equal(ok, PasswordPolicy.IsStrongEnough(password));

    [Fact]
    public void Five_wrong_passwords_lock_the_user_for_five_minutes()
    {
        var user = NewUser();
        for (var i = 0; i < User.MaxFailedLogins - 1; i++)
        {
            user.RegisterFailedLogin(Now);
        }

        Assert.False(user.IsLockedOut(Now));

        user.RegisterFailedLogin(Now);

        Assert.True(user.IsLockedOut(Now.AddMinutes(4)));
        Assert.False(user.IsLockedOut(Now.AddMinutes(5)));
    }

    [Fact]
    public void Successful_login_or_new_password_resets_the_count()
    {
        var user = NewUser();
        user.RegisterFailedLogin(Now);
        user.RegisterSuccessfulLogin(Now);
        Assert.Equal(0, user.FailedLoginCount);
        Assert.Equal(Now, user.LastLoginAt);

        for (var i = 0; i < User.MaxFailedLogins; i++)
        {
            user.RegisterFailedLogin(Now);
        }

        user.SetPasswordHash("new");
        Assert.False(user.IsLockedOut(Now));
    }

    [Fact]
    public void Password_hash_is_excluded_from_the_audit_log() =>
        Assert.NotNull(typeof(User).GetProperty(nameof(User.PasswordHash))!
            .GetCustomAttributes(typeof(Apteka.SharedKernel.AuditExcludedAttribute), false).SingleOrDefault());
}

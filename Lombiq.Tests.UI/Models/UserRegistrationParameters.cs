using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Pages;
using System;

namespace Lombiq.Tests.UI.Models;

public class UserRegistrationParameters
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
    public string LogInButtonText { get; set; } = OrchardCoreLoginPage.DefaultLoginButtonText;

    [Obsolete("Use CreateTest() instead.")]
    public static UserRegistrationParameters CreateDefault() =>
        new()
        {
            UserName = "TestUser",
            Email = "testuser@example.org",
            Password = DefaultUser.Password,
            ConfirmPassword = DefaultUser.Password,
        };

    public static UserRegistrationParameters CreateTest() =>
        new()
        {
            UserName = TestUser.UserName,
            Email = TestUser.Email,
            Password = TestUser.Password,
            ConfirmPassword = TestUser.Password,
        };
}

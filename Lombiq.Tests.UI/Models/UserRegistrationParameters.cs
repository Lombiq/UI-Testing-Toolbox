#nullable enable

using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Pages;
using System;

namespace Lombiq.Tests.UI.Models;

public record UserRegistrationParameters(
    string UserName,
    string Email,
    string Password,
    string? ConfirmPassword,
    string LoginButtonText = OrchardCoreLoginPage.DefaultLoginButtonText)
{
    [Obsolete("Use CreateTest() instead.")]
    public static UserRegistrationParameters CreateDefault() =>
        new("TestUser", "testuser@example.org", DefaultUser.Password, DefaultUser.Password);

    public static UserRegistrationParameters CreateTest() =>
        new(TestUser.UserName, TestUser.Email, TestUser.Password, TestUser.Password);
}

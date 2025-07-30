#nullable enable

using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Pages;
using System;

namespace Lombiq.Tests.UI.Models;

public record UserRegistrationParameters(
    string UserName,
    string Email,
    string Password = TestUser.Password,
    string? ConfirmPassword = TestUser.Password,
    string LoginButtonText = UserRegistrationParameters.DefaultLoginButtonText)
{
    public const string DefaultLoginButtonText = "Log in";

    [Obsolete("Use CreateTest() instead.")]
    public static UserRegistrationParameters CreateDefault() =>
        new("TestUser", "testuser@example.org");

    public static UserRegistrationParameters CreateTest(string loginButtonText = DefaultLoginButtonText) =>
        new(TestUser.UserName, TestUser.Email, LoginButtonText: loginButtonText);

    public static UserRegistrationParameters CreateDefaultUser(string loginButtonText = DefaultLoginButtonText) =>
        new(DefaultUser.UserName, DefaultUser.Email, LoginButtonText: loginButtonText);
}

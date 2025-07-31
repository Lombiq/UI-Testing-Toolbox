#nullable enable

using Atata;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

public record UserRegistrationParameters(
    string UserName,
    string Email,
    string Password = TestUser.Password,
    string? ConfirmPassword = TestUser.Password,
    string LoginButtonText = UserLoginParameters.DefaultLoginButtonText)
{
    public const string DefaultUrl = "Register";

    [Obsolete("Use CreateTest() instead.")]
    public static UserRegistrationParameters CreateDefault() =>
        new("TestUser", "testuser@example.org");

    public static UserRegistrationParameters CreateTest(string loginButtonText = UserLoginParameters.DefaultLoginButtonText) =>
        new(TestUser.UserName, TestUser.Email, LoginButtonText: loginButtonText);

    public static UserRegistrationParameters CreateDefaultUser(string loginButtonText = UserLoginParameters.DefaultLoginButtonText) =>
        new(DefaultUser.UserName, DefaultUser.Email, LoginButtonText: loginButtonText);

    public async Task RegisterAsync(UITestContext context, bool checkPrivacyConsent = true, bool navigate = true)
    {
        if (navigate)
        {
            await context.GoToRegistrationAsync();
        }

        if (checkPrivacyConsent &&
            context.Get(By.Id("RegisterUserForm_RegistrationCheckbox").Safely()) is { } privacyPolicyAgreement)
        {
            privacyPolicyAgreement.Click();
        }

        var password = ConfirmPassword ?? Password;

        await context.ClickAndFillInWithRetriesAsync(By.Id("RegisterUserForm_UserName"), UserName);
        await context.ClickAndFillInWithRetriesAsync(By.Id("RegisterUserForm_Email"), Email);
        await context.ClickAndFillInWithRetriesAsync(By.Id("RegisterUserForm_Password"), Password);
        await context.ClickAndFillInWithRetriesAsync(By.Id("RegisterUserForm_ConfirmPassword"), password);
        await context.ClickReliablyOnSubmitAsync();

        context.RefreshCurrentAtataContext();
    }
}

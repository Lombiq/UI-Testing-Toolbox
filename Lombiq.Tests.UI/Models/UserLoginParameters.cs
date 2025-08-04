using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Models;

public record UserLoginParameters(
    string UserName,
    string Password,
    string LoginButtonText = UserLoginParameters.DefaultLoginButtonText)
{
    public const string DefaultLoginButtonText = "Log in";

    public UserLoginParameters(UserRegistrationParameters parameters)
        : this(parameters.UserName, parameters.Password, parameters.LoginButtonText)
    {
    }

    public async Task LogInAsync(UITestContext context, bool navigate = true)
    {
        if (navigate) await context.GoToLoginAsync();

        var loginButtonText = string.IsNullOrEmpty(LoginButtonText) ? DefaultLoginButtonText : LoginButtonText;
        var userNameBy = By.Id("LoginForm_UserName");
        var passwordBy = By.Id("LoginForm_Password");

        try
        {
            await context.ClickAndFillInWithRetriesAsync(userNameBy, UserName);
            await context.ClickAndFillInWithRetriesAsync(passwordBy, Password);
        }
        catch (TimeoutException)
        {
            context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug(
                "Failed to fill in the login form, retrying with JavaScript.");

            await context.ClickAndFillInWithScriptAsync(userNameBy, UserName);
            await context.ClickAndFillInWithScriptAsync(passwordBy, Password);
        }

        var buttonBy = ByHelper.ButtonText(loginButtonText);

        try
        {
            await context.ClickReliablyOnUntilNavigationHasOccurredAsync(buttonBy);
        }
        catch (TimeoutException)
        {
            await context.ClickOnWithScriptAsync(buttonBy);
        }
    }

    public static implicit operator UserLoginParameters(UserRegistrationParameters parameters) =>
        new(parameters);
}

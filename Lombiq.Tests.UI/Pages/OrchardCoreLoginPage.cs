using Atata;
using Lombiq.Tests.UI.Components;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Pages;

// Atata convention.
#pragma warning disable IDE0065 // Misplaced using directive
using _ = OrchardCoreLoginPage;
#pragma warning restore IDE0065 // Misplaced using directive

[Url(DefaultUrl)]
[TermFindSettings(Case = TermCase.Pascal, TargetAllChildren = true, TargetAttributeType = typeof(FindByIdAttribute))]
[SuppressMessage(
    "Major Code Smell",
    "S1144:Unused private types or members should be removed",
    Justification = "Atata requires private setters: https://atata.io/examples/page-object-inheritance/.")]
public class OrchardCoreLoginPage : Page<_>
{
    private const string DefaultUrl = "Login";
    public const string DefaultLoginButtonText = "Log in";

    [FindById("LoginForm_UserName", nameof(UserName))]
    public TextInput<_> UserName { get; private set; }

    [FindById("LoginForm_Password", nameof(Password))]
    public PasswordInput<_> Password { get; private set; }

    [FindByAttribute("type", "submit")]
    public Button<_> LogIn { get; private set; }

    [FindByAttribute("href", TermMatch.Contains, "/" + OrchardCoreRegistrationPage.DefaultUrl)]
    public Link<OrchardCoreRegistrationPage, _> RegisterAsNewUser { get; private set; }

    public ValidationSummaryErrorList<_> ValidationSummaryErrors { get; private set; }

    public _ ShouldStayOnLoginPage() =>
        PageUrl.Should.StartWith(Context.BaseUrl + DefaultUrl);

    public _ ShouldLeaveLoginPage() =>
        PageUrl.Should.Not.StartWith(Context.BaseUrl + DefaultUrl);

    public async Task<_> LogInWithAsync(UITestContext context, string userName, string password, string loginButtonText = DefaultLoginButtonText)
    {
        if (string.IsNullOrEmpty(loginButtonText)) loginButtonText = DefaultLoginButtonText;

        var userNameBy = By.Id("LoginForm_UserName");
        var passwordBy = By.Id("LoginForm_Password");

        // The Atata input Set() and Click() are not always reliable in Chrome under Ubuntu, but sometimes even
        // ClickAndFillInWithRetriesAsync can fail and stuck failing, even with retried tests.
        try
        {
            await context.ClickAndFillInWithRetriesAsync(userNameBy, userName);
            await context.ClickAndFillInWithRetriesAsync(passwordBy, password);
        }
        catch (TimeoutException)
        {
            context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug(
                "Failed to fill in the login form, retrying with JavaScript.");

            await context.ClickAndFillInWithScriptAsync(userNameBy, userName);
            await context.ClickAndFillInWithScriptAsync(passwordBy, password);
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

        context.RefreshCurrentAtataContext();

        return this;
    }
}

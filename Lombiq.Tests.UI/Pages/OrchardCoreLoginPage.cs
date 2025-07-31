using Atata;
using Lombiq.Tests.UI.Components;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Pages;

[Url(DefaultUrl)]
[TermFindSettings(Case = TermCase.Pascal, TargetAllChildren = true, TargetAttributeType = typeof(FindByIdAttribute))]
[SuppressMessage(
    "Major Code Smell",
    "S1144:Unused private types or members should be removed",
    Justification = "Atata requires private setters: https://atata.io/examples/page-object-inheritance/.")]
[Obsolete("Classes inheriting from Page<> will be removed in the next version.")]
public class OrchardCoreLoginPage : Page<OrchardCoreLoginPage>
{
    private const string DefaultUrl = "Login";
    public const string DefaultLoginButtonText = UserLoginParameters.DefaultLoginButtonText;

    [FindById("LoginForm_UserName", nameof(UserName))]
    public TextInput<OrchardCoreLoginPage> UserName { get; private set; }

    [FindById("LoginForm_Password", nameof(Password))]
    public PasswordInput<OrchardCoreLoginPage> Password { get; private set; }

    [FindByAttribute("type", "submit")]
    public Button<OrchardCoreLoginPage> LogIn { get; private set; }

    [FindByAttribute("href", TermMatch.Contains, "/" + OrchardCoreRegistrationPage.DefaultUrl)]
    public Link<OrchardCoreRegistrationPage, OrchardCoreLoginPage> RegisterAsNewUser { get; private set; }

    public ValidationSummaryErrorList<OrchardCoreLoginPage> ValidationSummaryErrors { get; private set; }

    public OrchardCoreLoginPage ShouldStayOnLoginPage() =>
        PageUrl.Should.StartWith(Context.BaseUrl + DefaultUrl);

    public OrchardCoreLoginPage ShouldLeaveLoginPage() =>
        PageUrl.Should.Not.StartWith(Context.BaseUrl + DefaultUrl);

    public OrchardCoreLoginPage ShouldLeaveLoginPage(bool expected) =>
        expected ? ShouldLeaveLoginPage() : ShouldStayOnLoginPage();

    public Task<OrchardCoreLoginPage> LogInWithAsync(UITestContext context, UserRegistrationParameters parameters = null)
    {
        parameters ??= UserRegistrationParameters.CreateDefaultUser();
        return LogInWithAsync(context, parameters.UserName, parameters.Password, parameters.LoginButtonText);
    }

    public async Task<OrchardCoreLoginPage> LogInWithAsync(
        UITestContext context,
        string userName,
        string password,
        string loginButtonText = DefaultLoginButtonText)
    {
        await new UserLoginParameters(userName, password, loginButtonText).LogInAsync(context);

        context.RefreshCurrentAtataContext();
        return this;
    }
}

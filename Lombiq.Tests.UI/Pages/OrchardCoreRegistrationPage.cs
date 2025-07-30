using Atata;
using Lombiq.Tests.UI.Attributes.Behaviors;
using Lombiq.Tests.UI.Components;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Pages;

[Url(DefaultUrl)]
[TermFindSettings(Case = TermCase.Pascal, TargetAllChildren = true, TargetAttributeType = typeof(FindByNameAttribute))]
[SuppressMessage(
    "Major Code Smell",
    "S1144:Unused private types or members should be removed",
    Justification = "Atata requires private setters: https://atata.io/examples/page-object-inheritance/.")]
[Obsolete("Classes inheriting from Page<> will be removed in the next version.")]
public class OrchardCoreRegistrationPage : Page<OrchardCoreRegistrationPage>
{
    public const string DefaultUrl = "Register";

    [FindById("RegisterUserForm_UserName")]
    public TextInput<OrchardCoreRegistrationPage> UserName { get; private set; }

    [FindById("RegisterUserForm_Email")]
    [SetsValueReliably]
    public TextInput<OrchardCoreRegistrationPage> Email { get; private set; }

    [FindById("RegisterUserForm_Password")]
    public PasswordInput<OrchardCoreRegistrationPage> Password { get; private set; }

    [FindById("RegisterUserForm_ConfirmPassword")]
    public PasswordInput<OrchardCoreRegistrationPage> ConfirmPassword { get; private set; }

    [FindById("RegisterUserForm_RegistrationCheckbox")]
    public CheckBox<OrchardCoreRegistrationPage> PrivacyPolicyAgreement { get; private set; }

    [FindByAttribute("type", "submit")]
    public Button<OrchardCoreRegistrationPage> Register { get; private set; }

    public ValidationMessageList<OrchardCoreRegistrationPage> ValidationMessages { get; private set; }

    public OrchardCoreRegistrationPage ShouldStayOnRegistrationPage() =>
        PageUrl.Should.StartWith(Context.BaseUrl + DefaultUrl);

    public OrchardCoreRegistrationPage ShouldLeaveRegistrationPage() =>
        PageUrl.Should.Not.StartWith(Context.BaseUrl + DefaultUrl);

    public async Task<OrchardCoreRegistrationPage> RegisterWithAsync(
        UITestContext context, UserRegistrationParameters parameters, bool checkPrivacyConsent = true)
    {
        if (PrivacyPolicyAgreement.Exists() && checkPrivacyConsent)
        {
            PrivacyPolicyAgreement.Click();
        }

        // The Atata input Set() and Click() are not always reliable in Chrome under Ubuntu.
        await context.ClickAndFillInWithRetriesAsync(By.Id("RegisterUserForm_UserName"), parameters.UserName);
        await context.ClickAndFillInWithRetriesAsync(By.Id("RegisterUserForm_Email"), parameters.Email);
        await context.ClickAndFillInWithRetriesAsync(By.Id("RegisterUserForm_Password"), parameters.Password);
        await context.ClickAndFillInWithRetriesAsync(
            By.Id("RegisterUserForm_ConfirmPassword"),
            parameters.ConfirmPassword ?? parameters.Password);
        await context.ClickReliablyOnSubmitAsync();

        context.RefreshCurrentAtataContext();

        return this;
    }
}

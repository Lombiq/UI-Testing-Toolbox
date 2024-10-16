using Atata;
using Lombiq.Tests.UI.Attributes.Behaviors;
using Lombiq.Tests.UI.Components;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Pages;

// Atata convention.
#pragma warning disable IDE0065 // Misplaced using directive
using _ = OrchardCoreRegistrationPage;
#pragma warning restore IDE0065 // Misplaced using directive

[Url(DefaultUrl)]
[TermFindSettings(Case = TermCase.Pascal, TargetAllChildren = true, TargetAttributeType = typeof(FindByNameAttribute))]
[SuppressMessage(
    "Major Code Smell",
    "S1144:Unused private types or members should be removed",
    Justification = "Atata requires private setters: https://atata.io/examples/page-object-inheritance/.")]
public class OrchardCoreRegistrationPage : Page<_>
{
    public const string DefaultUrl = "Register";

    [FindById("RegisterUserForm_UserName")]
    public TextInput<_> UserName { get; private set; }

    [FindById("RegisterUserForm_Email")]
    [SetsValueReliably]
    public TextInput<_> Email { get; private set; }

    [FindById("RegisterUserForm_Password")]
    public PasswordInput<_> Password { get; private set; }

    [FindById("RegisterUserForm_ConfirmPassword")]
    public PasswordInput<_> ConfirmPassword { get; private set; }

    [FindById("RegisterUserForm_RegistrationCheckbox")]
    public CheckBox<_> PrivacyPolicyAgreement { get; private set; }

    [FindByAttribute("type", "submit")]
    public Button<_> Register { get; private set; }

    public ValidationMessageList<_> ValidationMessages { get; private set; }

    public _ ShouldStayOnRegistrationPage() =>
        PageUrl.Should.StartWith(Context.BaseUrl + DefaultUrl);

    public _ ShouldLeaveRegistrationPage() =>
        PageUrl.Should.Not.StartWith(Context.BaseUrl + DefaultUrl);

    public async Task<_> RegisterWithAsync(
        UITestContext context, UserRegistrationParameters parameters, bool checkPrivacyConsent = true)
    {
        UserName.Set(parameters.UserName);
        Email.Set(parameters.Email);
        Password.Set(parameters.Password);
        ConfirmPassword.Set(parameters.ConfirmPassword);

        if (PrivacyPolicyAgreement.Exists() && checkPrivacyConsent)
        {
            PrivacyPolicyAgreement.Click();
        }

        Register.Click();

        await context.TriggerAfterPageChangeEventAndRefreshAtataContextAsync();

        context.RefreshCurrentAtataContext();

        return this;
    }
}

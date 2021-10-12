using Atata;
using Lombiq.Tests.UI.Attributes.Behaviors;
using Lombiq.Tests.UI.Components;
using Lombiq.Tests.UI.Models;

namespace Lombiq.Tests.UI.Pages
{
    // Atata convention.
#pragma warning disable IDE0065 // Misplaced using directive
    using _ = OrchardCoreRegistrationPage;
#pragma warning restore IDE0065 // Misplaced using directive

    [Url(DefaultUrl)]
    [TermFindSettings(Case = TermCase.Pascal, TargetAllChildren = true, TargetAttributeType = typeof(FindByNameAttribute))]
    public class OrchardCoreRegistrationPage : Page<_>
    {
        public const string DefaultUrl = "Register";

        [FindByName]
        public TextInput<_> UserName { get; private set; }

        [FindByName]
        [SetsValueReliably]
        public TextInput<_> Email { get; private set; }

        [FindByName]
        public PasswordInput<_> Password { get; private set; }

        [FindByName]
        public PasswordInput<_> ConfirmPassword { get; private set; }

        [FindByName("RegistrationCheckbox")]
        public CheckBox<_> PrivacyPolicyAgreement { get; private set; }

        [FindByAttribute("type", "submit")]
        public Button<_> Register { get; private set; }

        public ValidationMessageList<_> ValidationMessages { get; private set; }

        public _ ShouldStayOnRegistrationPage() =>
            PageUrl.Should.StartWith(AtataContext.Current.BaseUrl + DefaultUrl);

        public _ ShouldLeaveRegistrationPage() =>
            PageUrl.Should.Not.StartWith(AtataContext.Current.BaseUrl + DefaultUrl);

        public _ RegisterWith(UserRegistrationModel model)
        {
            UserName.Set(model.UserName);
            Email.Set(model.Email);
            Password.Set(model.Password);
            ConfirmPassword.Set(model.ConfirmPassword);
            Register.Click();

            return this;
        }
    }
}

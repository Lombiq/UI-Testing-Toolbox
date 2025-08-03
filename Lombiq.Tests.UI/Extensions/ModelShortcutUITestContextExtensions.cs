using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class ModelShortcutUITestContextExtensions
{
    public static Task SetupOrchardCoreAsync(this UITestContext context, OrchardCoreSetupParameters parameters) =>
        parameters.SetupOrchardCoreAsync(context);

    public static Task LogInAsync(this UITestContext context, UserLoginParameters parameters, bool navigate = true) =>
        parameters.LogInAsync(context, navigate);

    public static Task RegisterAsync(
        this UITestContext context,
        UserRegistrationParameters parameters,
        bool checkPrivacyConsent = true,
        bool navigate = true) =>
        parameters.RegisterAsync(context, checkPrivacyConsent, navigate);
}

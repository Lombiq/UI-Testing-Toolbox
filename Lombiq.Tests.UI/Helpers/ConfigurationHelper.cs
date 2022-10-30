using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Helpers;

public static class ConfigurationHelper
{
    public static Func<OrchardCoreUITestExecutorConfiguration, Task> DisableHtmlValidation =>
        configuration =>
        {
            configuration.HtmlValidationConfiguration.RunHtmlValidationAssertionOnAllPageChanges = false;
            return Task.CompletedTask;
        };
}

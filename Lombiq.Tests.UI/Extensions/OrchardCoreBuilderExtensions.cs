using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrchardCore.Modules;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection;

public static class OrchardCoreBuilderExtensions
{
    /// <summary>
    /// Sets up everything for UI testing in the app if it was started during a test.
    /// </summary>
    /// <param name="configuration">
    /// The <see cref="IConfiguration"/> instance of the app where configuration options will be loaded from.
    /// </param>
    /// <param name="enableShortcutsDuringUITesting">
    /// A value indicating whether to enable the Lombiq.Tests.UI.Shortcuts feature. If set to <see langword="true"/> the
    /// feature will only be enabled if the web project references the Shortcuts module.
    /// </param>
    public static OrchardCoreBuilder ConfigureUITesting(
        this OrchardCoreBuilder builder,
        IConfiguration configuration,
        bool enableShortcutsDuringUITesting = false)
    {
        if (!configuration.IsUITesting()) return builder;

        // This allows running the app in the Development environment while UI testing. Otherwise
        // ModuleProjectStaticFileProvider would be active too, which tries to load static assets from local directories
        // as opposed to using the files embedded into the binaries. This can cause the tested app to load static files
        // from the original build directory which since then may contain the source code of a different version (thus
        // e.g. causing JS changes made in one branch to bleed through to the UI test execution of another branch).
        builder.ConfigureServices(services =>
            services
                .Replace(services.Single(service => service.ServiceType == typeof(IModuleStaticFileProvider)))
                .AddSingleton<IModuleStaticFileProvider>(serviceProvider =>
                    new ModuleEmbeddedStaticFileProvider(serviceProvider.GetRequiredService<IApplicationContext>())));

        if (enableShortcutsDuringUITesting) builder.AddTenantFeatures("Lombiq.Tests.UI.Shortcuts");

        var enableSmtp = configuration.GetValue<bool>("Lombiq_Tests_UI:EnableSmtpFeature");
        if (enableSmtp) builder.AddTenantFeatures("OrchardCore.Email.Smtp");

        if (configuration.GetValue<bool>("Lombiq_Tests_UI:UseAzureBlobStorage"))
        {
            builder.AddTenantFeatures("OrchardCore.Media.Azure.Storage");
        }

        return builder;
    }
}

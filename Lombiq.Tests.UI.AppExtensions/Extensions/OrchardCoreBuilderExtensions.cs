using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrchardCore.Email;
using OrchardCore.Media.Azure;
using OrchardCore.Modules;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OrchardCoreBuilderExtensions
    {
        public static OrchardCoreBuilder ConfigureUITesting(
            this OrchardCoreBuilder builder,
            IConfiguration configuration,
            bool enableShortcutsDuringUITesting = false)
        {
            if (!configuration.IsUITesting()) return builder;

            // This allows running the app in the Development environment while UI testing. Otherwise
            // ModuleProjectStaticFileProvider would be active too, which tries to load static assets from local
            // directories as opposed to using the files embedded into the binaries. This can cause the tested app to
            // load static files from the original build directory which since then may contain the source code of a
            // different version (thus e.g. causing JS changes made in one branch to bleed through to the UI test
            // execution of another branch).
            builder.ConfigureServices(services =>
                services
                    .Replace(services.Single(service => service.ServiceType == typeof(IModuleStaticFileProvider)))
                    .AddSingleton<IModuleStaticFileProvider>(serviceProvider =>
                        new ModuleEmbeddedStaticFileProvider(serviceProvider.GetRequiredService<IApplicationContext>())));

            if (enableShortcutsDuringUITesting) builder.AddTenantFeatures("Lombiq.Tests.UI.Shortcuts", "OrchardCore.Roles");

            var smtpPort = configuration.GetValue<string>("Lombiq_Tests_UI:SmtpSettings:Port");

            if (!string.IsNullOrEmpty(smtpPort)) builder.AddTenantFeatures("OrchardCore.Email");

            var blobStorageConnectionString = configuration
                .GetValue<string>("Lombiq_Tests_UI:MediaBlobStorageOptions:ConnectionString");

            if (!string.IsNullOrEmpty(blobStorageConnectionString))
            {
                builder.AddTenantFeatures("OrchardCore.Media.Azure.Storage");
            }

            return builder.ConfigureServices(
                services => services
                    .PostConfigure<SmtpSettings>(settings =>
                        configuration.GetSection("Lombiq_Tests_UI:SmtpSettings").Bind(settings))
                    .PostConfigure<MediaBlobStorageOptions>(options =>
                        configuration.GetSection("Lombiq_Tests_UI:MediaBlobStorageOptions").Bind(options)));
        }
    }
}

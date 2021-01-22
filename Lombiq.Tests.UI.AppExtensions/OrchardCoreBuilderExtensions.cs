using Microsoft.Extensions.Configuration;
using OrchardCore.Email;
using OrchardCore.Media.Azure;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OrchardCoreBuilderExtensions
    {
        public static OrchardCoreBuilder ConfigureUITesting(this OrchardCoreBuilder builder, IConfiguration configuration) =>
            builder.ConfigureServices(
                services => services
                    .PostConfigure<SmtpSettings>(settings => configuration.GetSection("Lombiq_Tests_UI_SmtpSettings").Bind(settings))
                    .PostConfigure<MediaBlobStorageOptions>(options =>
                        configuration.GetSection("Lombiq_Tests_UI_MediaBlobStorageOptions").Bind(options)));
    }
}

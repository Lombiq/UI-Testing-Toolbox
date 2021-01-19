using Microsoft.Extensions.Configuration;
using OrchardCore.Email;
using OrchardCore.Media.Azure;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureUITesting(this IServiceCollection services, IConfiguration configuration) =>
            services
                .PostConfigure<SmtpSettings>(settings => configuration.GetSection("Lombiq_Tests_UI_SmtpSettings").Bind(settings))
                .PostConfigure<MediaBlobStorageOptions>(options =>
                    configuration.GetSection("Lombiq_Tests_UI_MediaBlobStorageOptions").Bind(options));
    }
}

using Lombiq.Tests.UI.Shortcuts.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Lombiq.Tests.UI.Shortcuts
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services) =>
            services.Configure<MvcOptions>((options) => options.Filters.Add(typeof(ApplicationInfoInjectingFilter)));
    }
}

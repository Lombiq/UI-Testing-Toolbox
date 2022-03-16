using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OrchardCore.Modules;
using OrchardCore.ResourceManagement;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Services;

public class ApplicationInfoInjectingFilter : IAsyncResultFilter
{
    private readonly IResourceManager _resourceManager;
    private readonly IConfiguration _shellConfiguration;
    private readonly IApplicationContext _applicationContext;

    public ApplicationInfoInjectingFilter(
        IResourceManager resourceManager,
        IConfiguration shellConfiguration,
        IApplicationContext applicationContext)
    {
        _resourceManager = resourceManager;
        _shellConfiguration = shellConfiguration;
        _applicationContext = applicationContext;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.IsNotFullViewRendering() || !_shellConfiguration.GetValue("Lombiq_Tests_UI:InjectApplicationInfo", defaultValue: false))
        {
            await next();
            return;
        }

        _resourceManager.RegisterHeadScript(new HtmlString(
            $"<!--{Environment.NewLine}" +
            JsonConvert.SerializeObject(_applicationContext.GetApplicationInfo(), Formatting.Indented) +
            Environment.NewLine +
            "-->"));

        await next();
    }
}

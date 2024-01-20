using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OrchardCore.Modules;
using OrchardCore.ResourceManagement;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Services;

public class ApplicationInfoInjectingFilter(
    IResourceManager resourceManager,
    IConfiguration shellConfiguration,
    IApplicationContext applicationContext) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.IsNotFullViewRendering() || !shellConfiguration.GetValue("Lombiq_Tests_UI:InjectApplicationInfo", defaultValue: false))
        {
            await next();
            return;
        }

        resourceManager.RegisterHeadScript(new HtmlString(
            $"<!--{Environment.NewLine}" +
            JsonConvert.SerializeObject(applicationContext.GetApplicationInfo(), Formatting.Indented) +
            Environment.NewLine +
            "-->"));

        await next();
    }
}

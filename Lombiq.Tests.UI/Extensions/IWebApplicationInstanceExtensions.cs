using Lombiq.Tests.UI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Scope;
using OrchardCore.Recipes.Models;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class IWebApplicationInstanceExtensions
{
    /// <summary>
    /// Executes a delegate using the shell scope given by <paramref name="tenant"/> in an isolated async flow, while
    /// managing the shell state and invoking tenant events.
    /// </summary>
    public static async Task UsingScopeAsync(
        this IWebApplicationInstance instance,
        Func<ShellScope, Task> execute,
        string tenant = "Default",
        bool activateShell = true)
    {
        var shellHost = instance.GetService<IShellHost>();
        // Injecting a fake HttpContext is required to avoid NullReferenceException in
        // OrchardCore.Recipes.Services.RecipeEnvironmentFeatureProvider.PopulateEnvironmentAsync.
        var httpContextAccessor = instance.GetService<IHttpContextAccessor>();
        var features = new FeatureCollection();
        features.Set(new RecipeEnvironmentFeature());

        var httpContext = new DefaultHttpContext(features);
        httpContextAccessor.HttpContext = httpContext;

        try
        {
            await (await shellHost.GetScopeAsync(tenant))
                .UsingAsync(execute, activateShell);
        }
        finally
        {
            httpContextAccessor.HttpContext = null;
        }
    }
}

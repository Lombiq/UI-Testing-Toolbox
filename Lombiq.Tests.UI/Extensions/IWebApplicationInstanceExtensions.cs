using Lombiq.Tests.UI.Services;
using Microsoft.AspNetCore.Http;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Builders;
using OrchardCore.Environment.Shell.Scope;
using OrchardCore.Modules;
using OrchardCore.Recipes.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class IWebApplicationInstanceExtensions
{
    /// <summary>
    /// Executes a delegate using the shell scope given by <paramref name="tenant"/> in an isolated async flow, while
    /// managing the shell state and invoking tenant events.
    /// </summary>
    public static Task UsingScopeAsync(
        this IWebApplicationInstance instance,
        Func<IServiceProvider, Task> execute,
        string tenant = "Default",
        bool activateShell = true) =>
        instance.UsingScopeAsync(shellScope => execute(shellScope.ServiceProvider), tenant, activateShell);

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
        var shellHost = instance.GetRequiredService<IShellHost>();

        var httpContextAccessor = instance.GetRequiredService<IHttpContextAccessor>();
        var originalHttpContext = httpContextAccessor.HttpContext;

        try
        {
            // Injecting a fake HttpContext is required for many things, but it needs to happen before UsingAsync()
            // below to avoid NullReferenceExceptions in
            // OrchardCore.Recipes.Services.RecipeEnvironmentFeatureProvider.PopulateEnvironmentAsync. Migrations
            // (possibly with recipe migrations) run right at the shell start.

            var shellScope = await shellHost.GetScopeAsync(tenant);

            // Creating a fake HttpContext like in ModularBackgroundService.
            httpContextAccessor.HttpContext = shellScope.ShellContext.CreateHttpContext();
            var httpContext = httpContextAccessor.HttpContext;

            httpContext.Request.PathBase = "/" + shellHost.GetSettings(tenant).RequestUrlPrefix ?? string.Empty;
            httpContext.Features.Set(new RecipeEnvironmentFeature());

            await shellScope.UsingAsync(execute, activateShell);
        }
        finally
        {
            httpContextAccessor.HttpContext = originalHttpContext;
        }
    }
}

// This is from the 1.4 version of Orchard Core. During an Orchard upgrade to 1.5, remove this, and use the now public
// implementation in OrchardCore.Abstractions.
internal static class ShellExtensions
{
    public static HttpContext CreateHttpContext(this ShellContext shell)
    {
        var context = shell.Settings.CreateHttpContext();

        context.Features.Set(new ShellContextFeature
        {
            ShellContext = shell,
            OriginalPathBase = string.Empty,
            OriginalPath = "/",
        });

        return context;
    }

    public static HttpContext CreateHttpContext(this ShellSettings settings)
    {
        var context = new DefaultHttpContext().UseShellScopeServices();

        context.Request.Scheme = "https";

        var urlHost = settings.RequestUrlHost?.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        context.Request.Host = new HostString(urlHost ?? "localhost");

        if (!string.IsNullOrWhiteSpace(settings.RequestUrlPrefix))
        {
            context.Request.PathBase = "/" + settings.RequestUrlPrefix;
        }

        context.Request.Path = "/";
        context.Items["IsBackground"] = true;

        return context;
    }
}

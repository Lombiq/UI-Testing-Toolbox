using Lombiq.Tests.UI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.BackgroundTasks;
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
            httpContext.Request.PathBase = "/" + shellHost.GetSettings(tenant).RequestUrlPrefix;
            httpContext.Features.Set(new RecipeEnvironmentFeature());

            await shellScope.UsingAsync(execute, activateShell);
        }
        finally
        {
            httpContextAccessor.HttpContext = originalHttpContext;
        }
    }
}

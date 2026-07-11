using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using OrchardCore.Environment.Shell.Scope;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

[SuppressMessage(
    "Naming",
    "CA1708:Identifiers should differ by more than case",
    Justification = "Needed for backwards compatibility, remove after the next major release.")]
public static class FrontendUITestContextExtensions
{
    public const string FrontendPseudoTenantName = "!Frontend";

    /// <summary>
    /// Navigates to the backend <see cref="Uri"/> returned by <see
    /// cref="FrontendOrchardCoreUITestExecutorConfigurationExtensions.GetFrontendAndBackendUris"/> and presents it as
    /// switching to the Default tenant.
    /// </summary>
    /// <remarks><para>
    /// If the backend URL has not been initialized to something else (e.g. using a custom URL prefix), this is
    /// equivalent to using <see cref="UITestContext.SwitchCurrentTenantToDefault"/>. Even so, this method should be
    /// used for clarity when applicable.
    /// </para></remarks>
    public static void SwitchToBackend(this UITestContext context) =>
        context.SwitchCurrentTenant(
            tenantName: null,
            context.Configuration.GetFrontendAndBackendUris().BackendUri);

    /// <summary>
    /// Navigates to the frontend <see cref="Uri"/> returned by <see
    /// cref="FrontendOrchardCoreUITestExecutorConfigurationExtensions.GetFrontendAndBackendUris"/> and presents it as
    /// switching to a tenant named <see cref="FrontendPseudoTenantName"/>. This is not a real Orchard Core tenant, so
    /// this name can only be used for information (for example can't be used with <see
    /// cref="UsingScopeWebApplicationInstanceExtensions.UsingScopeAsync(IWebApplicationInstance,Func{ShellScope,Task},string,bool)"/>).
    /// </summary>
    public static void SwitchToFrontend(this UITestContext context) =>
        context.SwitchCurrentTenant(
            FrontendPseudoTenantName,
            context.Configuration.GetFrontendAndBackendUris().FrontendUri);

    public static string GetDriverPath(this UITestContext context)
    {
        if (context.Driver is not WebDriver { CommandExecutor: DriverServiceCommandExecutor executor })
        {
            throw new InvalidOperationException(
                $"The {nameof(GetDriverPath)} method requires a driver that inherits from {nameof(WebDriver)} and a " +
                $"command executor of type {nameof(DriverServiceCommandExecutor)}.");
        }

        var service = (DriverService)typeof(DriverServiceCommandExecutor)
            .GetField("service", BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(executor) ?? throw new InvalidOperationException("Couldn't get driver service.");

        return Path.Join(service.DriverServicePath, service.DriverServiceExecutableName);
    }
}

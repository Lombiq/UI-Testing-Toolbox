using CliWrap;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Extensions;

public static class FrontendUITestContextExtensions
{
    public const string FrontendPseudoTenantName = "!Frontend";

    /// <summary>
    /// Navigates to the backend <see cref="Uri"/> returned by <see
    /// cref="FrontendOrchardCoreUITestExecutorConfigurationExtensions.GetFrontendAndBackendUris"/> and presents it as
    /// switching to the default tenant.
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
    /// switching to a tenant named <see cref="FrontendPseudoTenantName"/> which is not a real Orchard Core tenant so
    /// this information can only be used for information.
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

    /// <summary>
    /// Executes the provided file via <c>node</c> with command line arguments containing the necessary information for
    /// Selenium JS to take over the browser.
    /// </summary>
    /// <param name="scriptPath">The Javascript source file to execute using <c>node</c>.</param>
    /// <param name="testOutputHelper">Needed to redirect the <c>node</c> output into the test logs.</param>
    public static async Task ExecuteJavascriptTestAsync(
        this UITestContext context,
        string scriptPath,
        ITestOutputHelper testOutputHelper)
    {
        const string command = "node";
        var pipe = testOutputHelper.ToPipeTarget($"{nameof(ExecuteJavascriptTestAsync)}({command})");

        try
        {
            await Cli.Wrap(command)
                .WithArguments([
                    "--inspect",
                    scriptPath,
                    context.GetDriverPath(),
                    context.Driver.Url,
                    context.GetTempSubDirectoryPath(),
                ])
                .WithStandardOutputPipe(pipe)
                .WithStandardErrorPipe(pipe)
                .ExecuteAsync();
        }
        catch
        {
            // The only reason this could throw if the above process call was not successful. In this case first check
            // the logs to throw a more specific exception if there is any.
            await context.TriggerAfterPageChangeEventAsync();
            throw;
        }
    }

    /// <summary>
    /// Creates a blank Node.js project in the current test session's <see cref="DirectoryPaths.Temp"/> directory and
    /// installs the provided NPM <paramref name="dependencies"/> using <c>pnpm</c>.
    /// </summary>
    public static async Task SetupNodeDependenciesAsync(this UITestContext context, ITestOutputHelper helper, params string[] dependencies)
    {
        var workingDirectory = context.GetTempSubDirectoryPath();
        var projectFilePath = Path.Join(workingDirectory, "package.json");

        if (!Directory.Exists(projectFilePath))
        {
            await File.WriteAllTextAsync(projectFilePath, "{ \"private\": true }");
        }

        var pipe = helper.ToPipeTarget(nameof(SetupNodeSeleniumAsync));
        await Cli.Wrap("pnpm")
            .WithArguments(["install", ..dependencies])
            .WithStandardOutputPipe(pipe)
            .WithStandardErrorPipe(pipe)
            .WithWorkingDirectory(workingDirectory)
            .ExecuteAsync();
    }

    /// <summary>
    /// Creates a blank Node.js project in the current test session's <see cref="DirectoryPaths.Temp"/> directory, then
    /// installs <c>selenium-webdriver</c> and any additional NPM dependencies using <c>pnpm</c>.
    /// </summary>
    public static Task SetupNodeSeleniumAsync(this UITestContext context, ITestOutputHelper helper, params string[] otherDependencies) =>
        context.SetupNodeDependenciesAsync(helper, ["selenium-webdriver", ..otherDependencies]);
}

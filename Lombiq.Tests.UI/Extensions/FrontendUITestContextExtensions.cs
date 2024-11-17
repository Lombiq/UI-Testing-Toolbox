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

    private static (string WorkingDirectory, string[] Arguments) GetExecuteJavascriptTestPats(
        this UITestContext context,
        string scriptPath,
        string workingDirectory)
    {
        workingDirectory = Path.GetFullPath(workingDirectory ?? Environment.CurrentDirectory);

        var absoluteScriptPath = Path.GetFullPath(scriptPath);
        var relativeScriptPath = Path.GetRelativePath(workingDirectory, scriptPath);

        var arguments = new[]
        {
            "--inspect",
            absoluteScriptPath.Length < relativeScriptPath.Length ? absoluteScriptPath : relativeScriptPath,
            context.GetDriverPath(),
            context.Driver.Url,
            context.GetTempSubDirectoryPath(),
        };

        return (workingDirectory, arguments);
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
        ITestOutputHelper testOutputHelper,
        string workingDirectory = null)
    {
        const string command = "node";
        var pipe = testOutputHelper.ToPipeTarget($"{nameof(ExecuteJavascriptTestAsync)}({command})");
        (workingDirectory, var arguments) = context.GetExecuteJavascriptTestPats(scriptPath, workingDirectory);

        try
        {
            await Cli.Wrap(command)
                .WithArguments(arguments)
                .WithStandardOutputPipe(pipe)
                .WithStandardErrorPipe(pipe)
                .WithWorkingDirectory(workingDirectory ?? Environment.CurrentDirectory)
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
    /// Invokes <see cref="ShortcutsUITestContextExtensions.SwitchToInteractiveAsync"/> with a custom notification
    /// message that contains instructions to invoke the Javascript test manually with <c>node</c>.
    /// </summary>
    /// <param name="scriptPath">The relative or absolute path pointing to the test script file.</param>
    /// <param name="workingDirectory">The path where the test script should be executed, will be converted to absolute.</param>
    public static Task SwitchToInteractiveWithJavascriptTestInfoAsync(
        this UITestContext context,
        string scriptPath,
        string workingDirectory = null)
    {
        (workingDirectory, var arguments) = context.GetExecuteJavascriptTestPats(scriptPath, workingDirectory);

        return context.SwitchToInteractiveAsync(
            $"To start a Javascript test, open a command line terminal at \"{workingDirectory}\" and type the " +
            $"following command: <code class=\"d-block\">node {string.Join(' ', arguments)}</code>");
    }

    /// <summary>
    /// Sets up the Javascript dependencies using <see cref="SetupNodeSeleniumAsync"/> and then runs the script in the
    /// same temp directory.
    /// </summary>
    /// <param name="scriptPath">
    /// The path of the Javascript file to execute with <c>node</c>. Before passing it to <see
    /// cref="ExecuteJavascriptTestAsync"/>, it's transformed into a relative path based on the temp directory to
    /// conserve path length because long paths can be a problem in some operating systems.
    /// </param>
    public static async Task SetupSeleniumAndExecuteJavascriptTestAsync(
        this UITestContext context,
        string scriptPath,
        ITestOutputHelper testOutputHelper,
        params string[] otherDependencies)
    {
        var workingDirectory = await context.SetupNodeSeleniumAsync(testOutputHelper, otherDependencies);
        var relativePath = Path.GetRelativePath(workingDirectory, scriptPath);

        await context.ExecuteJavascriptTestAsync(relativePath, testOutputHelper, workingDirectory);
    }

    /// <summary>
    /// Creates a blank Node.js project in the current test session's <see cref="DirectoryPaths.Temp"/> directory and
    /// installs the provided NPM <paramref name="dependencies"/> using <c>pnpm</c>.
    /// </summary>
    /// <returns>The path of the directory where the project is set up.</returns>
    public static async Task<string> SetupNodeDependenciesAsync(
        this UITestContext context,
        ITestOutputHelper helper,
        params string[] dependencies)
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

        return workingDirectory;
    }

    /// <summary>
    /// Creates a blank Node.js project in the current test session's <see cref="DirectoryPaths.Temp"/> directory, then
    /// installs <c>selenium-webdriver</c> and any additional NPM dependencies using <c>pnpm</c>.
    /// </summary>
    /// <returns>The path of the directory where the project is set up.</returns>
    public static Task<string> SetupNodeSeleniumAsync(
        this UITestContext context,
        ITestOutputHelper helper,
        params string[] otherDependencies) =>
        context.SetupNodeDependenciesAsync(helper, ["selenium-webdriver", ..otherDependencies]);
}

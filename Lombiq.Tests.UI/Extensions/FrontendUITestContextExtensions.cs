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
                    DirectoryPaths.GetTempSubDirectoryPath(context.Id),
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
}

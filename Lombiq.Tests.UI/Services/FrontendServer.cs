#nullable enable

using CliWrap;
using Lombiq.HelpfulLibraries.Common.Utilities;
using Lombiq.Tests.UI.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Services;

/// <summary>
/// Manages an external process which serves the frontend and acts as an intermediary between the browser and a headless
/// Orchard Core instance.
/// </summary>
public class FrontendServer
{
    private readonly OrchardCoreUITestExecutorConfiguration _configuration;
    private readonly ITestOutputHelper _testOutputHelper;

    public string Name { get; }

    public FrontendServer(
        string name,
        OrchardCoreUITestExecutorConfiguration configuration,
        ITestOutputHelper testOutputHelper)
    {
        _configuration = configuration;
        _testOutputHelper = testOutputHelper;
        Name = name;
    }

    /// <summary>
    /// Configures the Orchard Core startup and shutdown events to manage the frontend server's lifecycle. Also
    /// redirects the standard output and error streams to the <see cref="ITestOutputHelper"/>.
    /// </summary>
    /// <param name="program">The path of the frontend server application.</param>
    /// <param name="arguments">Additional command line arguments to be passed to <paramref name="program"/>.</param>
    /// <param name="configureCommand">Additional <c>Cli.Wrap</c> configuration for <paramref name="program"/>.</param>
    /// <param name="checkProgramReady">
    /// If not <see langword="null"/>, it checks every line from the standard output or error streams and waits until
    /// this function returns <see langword="true"/>.
    /// </param>
    /// <param name="thenAsync">
    /// If not <see langword="null"/>, it's executed at the end of the <see
    /// cref="OrchardCoreConfiguration.BeforeAppStart"/> handler that this method adds.
    /// </param>
    /// <param name="skipStartup">
    /// If not <see langword="null"/> and returns <see langword="true"/>, the <see
    /// cref="OrchardCoreConfiguration.BeforeAppStart"/> event handler won't start up the specified program (and so
    /// <paramref name="configureCommand"/> and <paramref name="checkProgramReady"/> are ignored as well). This is
    /// useful if you don't need the frontend server during setup. Note that if <paramref name="thenAsync"/> is
    /// specified, it will be invoked regardless. This way configuration changes are still provided but the performance
    /// impact of an unnecessary additional process is mitigated.
    /// </param>
    public void Configure(
        string program,
        IEnumerable<string>? arguments = null,
        Func<Command, Context, Command>? configureCommand = null,
        Func<string, Context, bool>? checkProgramReady = null,
        Func<Context, Task>? thenAsync = null,
        Func<Context, bool>? skipStartup = null,
        TimeSpan? startupTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(program);
        skipStartup ??= _ => false;

        var cli = Cli.Wrap(program).WithArguments(arguments ?? []);

        _configuration.OrchardCoreConfiguration.BeforeAppStart += async (orchardContext, orchardArguments) =>
        {
            var frontendPort = await orchardContext.PortLeaseManager.LeaseAvailableRandomPortAsync();
            var context = new Context(
                orchardContext.ContentRootPath,
                orchardContext.Url,
                orchardContext.PortLeaseManager,
                frontendPort,
                orchardArguments);

            var cancellationTokenSource = new CancellationTokenSource();
            var waitCompletionSource = new TaskCompletionSource();
            var execute = !skipStartup(context);
            var waiting = execute && checkProgramReady != null;

            var pipe = PipeTarget.ToDelegate(line =>
            {
                _testOutputHelper.WriteOutputTimestampedAndDebug(Name, line);
                if (waiting && checkProgramReady!(line, context)) waitCompletionSource.SetResult();
            });

            if (!execute)
            {
                await thenAsync.InvokeFuncAsync(context);
                return;
            }

            cli = configureCommand?.Invoke(cli, context) ?? cli;
            var cliTask = cli
                .WithStandardOutputPipe(pipe)
                .WithStandardErrorPipe(pipe)
                .ExecuteAsync(cancellationTokenSource.Token);

            if (waiting) await WaitForStartupAsync(cliTask, waitCompletionSource.Task, startupTimeout);

            _configuration.CustomConfiguration[GetKey(context.Url.Port)] = new FrontendServerContext
            {
                Port = frontendPort,
                Task = cliTask,
                StopAsync = async () =>
                {
                    // This cancellation token forcefully closes the frontend server (i.e. SIGTERM, Ctrl+C), which is
                    // the only way to shut down most of these servers anyway. For this reason there is no need to await
                    // the task, and trying to do so would throw OperationCanceledException.
                    await cancellationTokenSource.CancelAsync();
                    cancellationTokenSource.Dispose();

                    await context.PortLeaseManager.StopLeaseAsync(frontendPort);
                },
            };

            await thenAsync.InvokeFuncAsync(context);
        };

        _configuration.OrchardCoreConfiguration.AfterAppStop += context =>
            GetContext(context.Url.Port) is { StopAsync: { } stopAsync }
                ? stopAsync()
                : Task.CompletedTask;
    }

    public FrontendServerContext? GetContext(int orchardPort) =>
        _configuration.CustomConfiguration.GetMaybe(GetKey(orchardPort)) as FrontendServerContext;

    public FrontendServerContext? GetContext(UITestContext context) => GetContext(context.TestStartUri.Port);

    private string GetKey(int orchardPort) =>
        StringHelper.CreateInvariant($"{nameof(FrontendServer)}:{Name}:{orchardPort}");

    private static async Task WaitForStartupAsync(Task mainTask, Task waitTask, TimeSpan? timeout)
    {
        var tasks = new List<Task>(capacity: 3) { mainTask, waitTask };

        Task? timeoutTask = null;
        if (timeout.HasValue)
        {
            timeoutTask = Task.Delay(timeout.Value, default(CancellationToken));
            tasks.Add(timeoutTask);
        }

        // Use WhenAny in case the CLI task fails before the wait task completes. This prevents hangs.
        await Task.WhenAny(tasks).Unwrap();

        if (timeoutTask?.IsCompleted == true)
        {
            throw new TimeoutException(StringHelper.CreateInvariant(
                $"The timeout of {nameof(FrontendServer)} ({timeout}) is exceeded."));
        }
    }

    public record Context(
        string ContentRootPath,
        Uri Url,
        PortLeaseManager PortLeaseManager,
        int FrontendPort,
        InstanceCommandLineArgumentsBuilder OrchardCoreArguments)
        : OrchardCoreAppStartContext(ContentRootPath, Url, PortLeaseManager);
}

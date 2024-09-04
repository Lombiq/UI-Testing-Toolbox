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
    /// <param name="thenAsync">If not <see langword="null"/>, it's executed at the end </param>
    public void Configure(
        string program,
        IEnumerable<string>? arguments = null,
        Func<Command, Context, Command>? configureCommand = null,
        Func<string, Context, bool>? checkProgramReady = null,
        Func<Context, Task>? thenAsync = null)
    {
        ArgumentNullException.ThrowIfNull(program);

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
            var waiting = checkProgramReady != null;

            var pipe = PipeTarget.ToDelegate(line =>
            {
                _testOutputHelper.WriteLineTimestamped("{0}: {1}", Name, line);
                if (waiting && checkProgramReady!(line, context)) waitCompletionSource.SetResult();
            });

            var cli = Cli.Wrap(program)
                .WithStandardOutputPipe(pipe)
                .WithStandardOutputPipe(pipe);
            if (arguments != null) cli = cli.WithArguments(arguments);
            if (configureCommand != null) cli = configureCommand(cli, context);
            var task = cli.ExecuteAsync(cancellationTokenSource.Token);

            if (waiting) await waitCompletionSource.Task;

            _configuration.CustomConfiguration[GetKey(context)] = new FrontendServerContext
            {
                Port = frontendPort,
                Task = task,
                Stop = async () =>
                {
                    await cancellationTokenSource.CancelAsync();
                    await task; // Wait for this process to stop cleanly.
                    cancellationTokenSource.Dispose();

                    await context.PortLeaseManager.StopLeaseAsync(frontendPort);
                },
            };
        };

        _configuration.OrchardCoreConfiguration.AfterAppStop += context =>
            _configuration.CustomConfiguration[GetKey(context)] is FrontendServerContext { Stop: { } stopAsync }
                ? stopAsync()
                : Task.CompletedTask;
    }

    private string GetKey(OrchardCoreAppStartContext context) =>
        StringHelper.CreateInvariant($"{nameof(FrontendServer)}:{Name}:{context.Url.Port}");

    public record Context(
        string ContentRootPath,
        Uri Url,
        PortLeaseManager PortLeaseManager,
        int FrontendPort,
        InstanceCommandLineArgumentsBuilder OrchardCoreArguments)
        : OrchardCoreAppStartContext(ContentRootPath, Url, PortLeaseManager);
}

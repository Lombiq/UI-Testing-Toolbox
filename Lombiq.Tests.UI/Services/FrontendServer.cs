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

    public void Configure(string program, IEnumerable<string>? arguments = null, Func<Command, Command>? configureCommand = null)
    {
        ArgumentNullException.ThrowIfNull(program);

        _configuration.OrchardCoreConfiguration.BeforeAppStart += async (context, _) =>
        {
            var frontendPort = await context.PortLeaseManager.LeaseAvailableRandomPortAsync();

            var cli = Cli.Wrap(program)
                .WithStandardOutputPipe(PipeTarget.ToDelegate(HandleLine))
                .WithStandardOutputPipe(PipeTarget.ToDelegate(HandleLine));
            if (arguments != null) cli = cli.WithArguments(arguments);
            if (configureCommand != null) cli = configureCommand(cli);

            var cancellationTokenSource = new CancellationTokenSource();
            var task = cli.ExecuteAsync(cancellationTokenSource.Token);

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

    private void HandleLine(string line) => _testOutputHelper.WriteLineTimestamped("{0}: {1}", Name, line);

    private string GetKey(OrchardCoreAppStartContext context) =>
        StringHelper.CreateInvariant($"{nameof(FrontendServer)}:{Name}:{context.Url.Port}");
}

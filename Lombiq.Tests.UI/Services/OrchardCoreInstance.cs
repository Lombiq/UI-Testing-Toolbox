using Lombiq.Tests.Integration.Services;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services.OrchardCoreHosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic.FileIO;
using OrchardCore.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Services;

public delegate Task BeforeAppStartHandler(string contentRootPath, InstanceCommandLineArgs arguments);

public delegate Task BeforeTakeSnapshotHandler(string contentRootPath, string snapshotDirectoryPath);

public class OrchardCoreConfiguration
{
    public string AppAssemblyPath { get; set; }
    public string SnapshotDirectoryPath { get; set; }
    public BeforeAppStartHandler BeforeAppStart { get; set; }
    public BeforeTakeSnapshotHandler BeforeTakeSnapshot { get; set; }

    /// <summary>
    /// Adds a command line argument to the app during <see cref="BeforeAppStart"/> that switches AI into offline mode.
    /// This way it won't try to reach out to a remote server with telemetry and the test remains self-sufficient.
    /// </summary>
    public void EnableApplicationInsightsOfflineOperation() =>
        BeforeAppStart +=
            (_, argumentsBuilder) =>
            {
                argumentsBuilder
                    .AddValue("OrchardCore:Lombiq_Hosting_Azure_ApplicationInsights:EnableOfflineOperation", value: true);

                return Task.CompletedTask;
            };
}

internal static class OrchardCoreInstanceCounter
{
    public const string UrlPrefix = "https://localhost:";

#pragma warning disable IDE0079 // Remove unnecessary suppression
    [SuppressMessage(
        "Performance",
        "CA1810:Initialize reference type static fields inline",
        Justification = "No GetAgentIndexOrDefault() duplication this way.")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
    static OrchardCoreInstanceCounter()
    {
        var agentIndexTimesHundred = TestConfigurationManager.GetAgentIndexOrDefault() * 100;
        PortLeases = new PortLeaseManager(9000 + agentIndexTimesHundred, 9099 + agentIndexTimesHundred);
    }

    public static PortLeaseManager PortLeases { get; private set; }
}

/// <summary>
/// A locally executing Orchard Core application.
/// </summary>
public sealed class OrchardCoreInstance<TEntryPoint> : IWebApplicationInstance
    where TEntryPoint : class
{
    private readonly OrchardCoreConfiguration _configuration;
    private readonly string _contextId;
    private readonly ITestOutputHelper _testOutputHelper;
    private string _contentRootPath;
    private bool _isDisposed;
    private OrchardApplicationFactory<TEntryPoint> _orchardApplication;
    private string _url;
    private TestReverseProxy _reverseProxy;

    public OrchardCoreInstance(OrchardCoreConfiguration configuration, string contextId, ITestOutputHelper testOutputHelper)
    {
        _configuration = configuration;
        _contextId = contextId;
        _testOutputHelper = testOutputHelper;
    }

    public async Task<Uri> StartUpAsync()
    {
        var port = OrchardCoreInstanceCounter.PortLeases.LeaseAvailableRandomPort();
        _url = OrchardCoreInstanceCounter.UrlPrefix + port.ToTechnicalString();
        _testOutputHelper.WriteLineTimestampedAndDebug("The generated URL for the Orchard Core instance is \"{0}\".", _url);

        CreateContentRootFolder();

        if (!string.IsNullOrEmpty(_configuration.SnapshotDirectoryPath) && Directory.Exists(_configuration.SnapshotDirectoryPath))
        {
            FileSystem.CopyDirectory(_configuration.SnapshotDirectoryPath, _contentRootPath, overwrite: true);
        }
        else
        {
            // Copying the config files from the assembly path, i.e. the build output path so only those are included
            // that actually matter.
            OrchardCoreDirectoryHelper
                .CopyAppConfigFiles(Path.GetDirectoryName(_configuration.AppAssemblyPath), _contentRootPath);
        }

        _reverseProxy = new TestReverseProxy(_url);

        await _reverseProxy.StartAsync();

        await StartOrchardAppAsync();

        return new Uri(_url);
    }

    public Task PauseAsync() => StopOrchardAppAsync();

    public Task ResumeAsync() => StartOrchardAppAsync();

    public async Task TakeSnapshotAsync(string snapshotDirectoryPath)
    {
        ArgumentNullException.ThrowIfNull(snapshotDirectoryPath);

        await PauseAsync();

        if (Directory.Exists(snapshotDirectoryPath)) Directory.Delete(snapshotDirectoryPath, recursive: true);

        Directory.CreateDirectory(snapshotDirectoryPath);

        await _configuration.BeforeTakeSnapshot
            .InvokeAsync<BeforeTakeSnapshotHandler>(handler => handler(_contentRootPath, snapshotDirectoryPath));

        FileSystem.CopyDirectory(_contentRootPath, snapshotDirectoryPath, overwrite: true);
    }

    public IEnumerable<IApplicationLog> GetLogs(CancellationToken cancellationToken = default)
    {
        if (cancellationToken == default) cancellationToken = CancellationToken.None;

        var logFolderPath = Path.Combine(_contentRootPath, "App_Data", "logs");
        return Directory.Exists(logFolderPath)
            ? Directory
                .EnumerateFiles(logFolderPath, "*.log")
                .Select(filePath => (IApplicationLog)new ApplicationLog
                {
                    Name = Path.GetFileName(filePath),
                    FullName = Path.GetFullPath(filePath),
                    ContentLoader = () => File.ReadAllTextAsync(filePath, cancellationToken),
                })
            : Enumerable.Empty<IApplicationLog>();
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        await StopOrchardAppAsync();
        if (_reverseProxy != null)
        {
            await _reverseProxy.DisposeAsync();
        }

        DirectoryHelper.SafelyDeleteDirectoryIfExists(_contentRootPath, 60);
    }

    private void CreateContentRootFolder()
    {
        _contentRootPath = DirectoryPaths.GetTempSubDirectoryPath(_contextId, "App");
        Directory.CreateDirectory(_contentRootPath);
        _testOutputHelper.WriteLineTimestampedAndDebug("Content root path was created: {0}", _contentRootPath);
    }

    private async Task StartOrchardAppAsync()
    {
        _testOutputHelper.WriteLineTimestampedAndDebug("Attempting to start the Orchard Core instance.");

        var arguments = new InstanceCommandLineArgs();

        await _configuration.BeforeAppStart
            .InvokeAsync<BeforeAppStartHandler>(handler => handler(_contentRootPath, arguments));

        _orchardApplication = new OrchardApplicationFactory<TEntryPoint>(
            builder => builder
                .UseContentRoot(_contentRootPath)
                .UseWebRoot(Path.Combine(_contentRootPath, "wwwroot"))
                .UseEnvironment("Development")
                .ConfigureAppConfiguration(configuration => configuration.AddCommandLine(arguments.Args.ToArray())),
            (configuration, orchardBuilder) => orchardBuilder
                .ConfigureUITesting(configuration, enableShortcutsDuringUITesting: true));

        _orchardApplication.ClientOptions.AllowAutoRedirect = false;
        _orchardApplication.ClientOptions.BaseAddress = new Uri(_reverseProxy.RootUrl);
        _reverseProxy.AttachConnectionProvider(_orchardApplication);

        _testOutputHelper.WriteLineTimestampedAndDebug("The Orchard Core instance was started.");
    }

    private async Task StopOrchardAppAsync()
    {
        _reverseProxy.DetachConnectionProvider();
        if (_orchardApplication == null) return;

        _testOutputHelper.WriteLineTimestampedAndDebug("Attempting to stop the Orchard Core instance.");

        await _orchardApplication.DisposeAsync();
        _orchardApplication = null;

        _testOutputHelper.WriteLineTimestampedAndDebug("The Orchard Core instance was stopped.");

        return;
    }

    private class ApplicationLog : IApplicationLog
    {
        public string Name { get; init; }
        public string FullName { get; init; }
        public Func<Task<string>> ContentLoader { get; init; }

        public Task<string> GetContentAsync() => ContentLoader();

        public void Remove()
        {
            if (File.Exists(FullName)) File.Delete(FullName);
        }
    }
}

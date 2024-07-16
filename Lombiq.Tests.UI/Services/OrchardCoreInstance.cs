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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Services;

public delegate Task BeforeAppStartHandler(string contentRootPath, InstanceCommandLineArgumentsBuilder arguments);

public delegate Task BeforeTakeSnapshotHandler(string contentRootPath, string snapshotDirectoryPath);

public class OrchardCoreConfiguration
{
    private string _basePath;

    public string SnapshotDirectoryPath { get; set; }

    /// <summary>
    /// Gets or sets the base path of the application if mapped to a different URL. For example, if the Orchard Core
    /// application is mapped to "/cms" then this should be "/cms".
    /// </summary>
    public string BasePath
    {
        get => _basePath;
        set => _basePath = value.TrimEnd('/') + '/';
    }

    public BeforeAppStartHandler BeforeAppStart { get; set; }
    public BeforeTakeSnapshotHandler BeforeTakeSnapshot { get; set; }
}

internal static class OrchardCoreInstanceCounter
{
    public const string UrlPrefix = "https://localhost:";

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

    public IServiceProvider Services => _orchardApplication?.Services;

    public OrchardCoreInstance(OrchardCoreConfiguration configuration, string contextId, ITestOutputHelper testOutputHelper)
    {
        _configuration = configuration;
        _contextId = contextId;
        _testOutputHelper = testOutputHelper;
    }

    public async Task<Uri> StartUpAsync()
    {
        var port = await OrchardCoreInstanceCounter.PortLeases.LeaseAvailableRandomPortAsync();
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
                .CopyAppConfigFiles(
                    Path.GetDirectoryName(typeof(TEntryPoint).Assembly.Location),
                    _contentRootPath);
        }

        _reverseProxy = new TestReverseProxy(_url, _configuration.BasePath);

        await _reverseProxy.StartAsync();

        await StartOrchardAppAsync();

        return new Uri(new Uri(_url), _configuration.BasePath);
    }

    public Task PauseAsync() => StopOrchardAppAsync();

    public Task ResumeAsync() => StartOrchardAppAsync();

    public Task TakeSnapshotAsync(string snapshotDirectoryPath)
    {
        ArgumentNullException.ThrowIfNull(snapshotDirectoryPath);

        return TakeSnapshotInnerAsync(snapshotDirectoryPath);
    }

    public IEnumerable<IApplicationLog> GetLogs(CancellationToken cancellationToken = default)
    {
        var logFolderPath = Path.Combine(_contentRootPath, "App_Data", "logs");
        return Directory.Exists(logFolderPath)
            ? Directory
                .EnumerateFiles(logFolderPath, "*.log")
                .Select(filePath => (IApplicationLog)new ApplicationLog
                {
                    Name = Path.GetFileName(filePath),
                    FullName = Path.GetFullPath(filePath),
                    ContentLoader = () => GetFileContentAsync(filePath, cancellationToken),
                })
            : [];
    }

    public TService GetRequiredService<TService>() =>
        _orchardApplication.Services.GetRequiredService<TService>();

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

        var arguments = new InstanceCommandLineArgumentsBuilder();

        await _configuration.BeforeAppStart
            .InvokeAsync<BeforeAppStartHandler>(handler => handler(_contentRootPath, arguments));

        // This is to avoid adding Razor runtime view compilation.
        DirectoryHelper.SafelyDeleteDirectoryIfExists(
            Path.Combine(Path.GetDirectoryName(typeof(OrchardCoreInstance<>).Assembly.Location), "refs"), 60);

        _orchardApplication = new OrchardApplicationFactory<TEntryPoint>(
            configuration =>
                configuration.AddCommandLine(arguments.Arguments.ToArray()),
            builder => builder
                .UseContentRoot(_contentRootPath)
                .UseWebRoot(Path.Combine(_contentRootPath, "wwwroot"))
                .UseEnvironment(Environments.Development),
            (configuration, orchardBuilder) => orchardBuilder
                .ConfigureUITesting(configuration, enableShortcutsDuringUITesting: true));

        _orchardApplication.ClientOptions.AllowAutoRedirect = false;
        _orchardApplication.ClientOptions.BaseAddress = new Uri(_reverseProxy.RootUrl);
        _reverseProxy.AttachConnectionProvider(_orchardApplication);

        _testOutputHelper.WriteLineTimestampedAndDebug("The Orchard Core instance was started.");
    }

    private async Task StopOrchardAppAsync()
    {
        _reverseProxy?.DetachConnectionProvider();

        if (_orchardApplication == null) return;

        _testOutputHelper.WriteLineTimestampedAndDebug("Attempting to stop the Orchard Core instance.");

        await _orchardApplication.DisposeAsync();
        _orchardApplication = null;

        _testOutputHelper.WriteLineTimestampedAndDebug("The Orchard Core instance was stopped.");

        return;
    }

    private static async Task<string> GetFileContentAsync(string filePath, CancellationToken cancellationToken)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var streamReader = new StreamReader(fileStream);
        return await streamReader.ReadToEndAsync(cancellationToken);
    }

    private async Task TakeSnapshotInnerAsync(string snapshotDirectoryPath)
    {
        await PauseAsync();

        if (Directory.Exists(snapshotDirectoryPath)) Directory.Delete(snapshotDirectoryPath, recursive: true);

        Directory.CreateDirectory(snapshotDirectoryPath);

        await _configuration.BeforeTakeSnapshot
            .InvokeAsync<BeforeTakeSnapshotHandler>(handler => handler(_contentRootPath, snapshotDirectoryPath));

        FileSystem.CopyDirectory(_contentRootPath, snapshotDirectoryPath, overwrite: true);
    }

    private sealed class ApplicationLog : IApplicationLog
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

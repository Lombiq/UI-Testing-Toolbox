using CliWrap;
using Lombiq.HelpfulLibraries.Cli;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using YamlDotNet.Serialization;

namespace Lombiq.Tests.UI.SecurityScanning;

/// <summary>
/// Service to manage Zed Attack Proxy (ZAP, see https://www.zaproxy.org/) instances and security scans.
/// </summary>
public sealed class ZapManager : IAsyncDisposable
{
    // Need to use the weekly release because that's the one that has packaged scans migrated to Automation Framework.
    // When updating this version, also regenerate the Automation Framework YAML config files so we don't miss any
    // changes to those.
    private const string _zapImage = "softwaresecurityproject/zap-weekly:20231113";

    private static readonly SemaphoreSlim _pullSemaphore = new(1, 1);
    private static readonly CliProgram _docker = new("docker");

    private readonly ITestOutputHelper _testOutputHelper;

    private static bool _wasPulled;

    private CancellationTokenSource _cancellationTokenSource;

    internal ZapManager(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

    /// <summary>
    /// Run a Zed Attack Proxy (ZAP, see https://www.zaproxy.org/) security scan against an app.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> of the currently executing test.</param>
    /// <param name="automationFrameworkYamlPath">
    /// File system path to the YAML configuration file of ZAP's Automation Framework. See
    /// <see href="https://www.zaproxy.org/docs/automate/automation-framework/"/> for details.
    /// </param>
    /// <param name="startUri">The <see cref="Uri"/> under the app where to start the scan from.</param>
    /// <param name="modifyYaml">
    /// A delegate that may optionally modify the deserialized representation of the ZAP Automation Framework YAML.
    /// </param>
    public Task RunSecurityScanAsync(
        UITestContext context,
        string automationFrameworkYamlPath,
        Uri startUri,
        Func<object, Task> modifyYaml = null) =>
        RunSecurityScanAsync(
            context,
            automationFrameworkYamlPath,
            async configuration =>
            {
                SetStartUrlInYaml(configuration, startUri);
                if (modifyYaml != null) await modifyYaml(configuration);
            });

    /// <summary>
    /// Run a Zed Attack Proxy (ZAP, see https://www.zaproxy.org/) security scan against an app.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> of the currently executing test.</param>
    /// <param name="automationFrameworkYamlPath">
    /// File system path to the YAML configuration file of ZAP's Automation Framework. See
    /// <see href="https://www.zaproxy.org/docs/automate/automation-framework/"/> for details.
    /// </param>
    /// <param name="modifyYaml">
    /// A delegate that may optionally modify the deserialized representation of the ZAP Automation Framework YAML.
    /// </param>
    public async Task RunSecurityScanAsync(
        UITestContext context,
        string automationFrameworkYamlPath,
        Func<object, Task> modifyYaml = null)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrEmpty(automationFrameworkYamlPath))
        {
            automationFrameworkYamlPath = AutomationFrameworkYamlPaths.BaselineYamlPath;
        }

        var mountedDirectoryPath = DirectoryPaths.GetTempSubDirectoryPath(context.Id, "Zap");
        Directory.CreateDirectory(mountedDirectoryPath);

        var yamlFileName = Path.GetFileName(automationFrameworkYamlPath);
        var yamlFileCopyPath = Path.Combine(mountedDirectoryPath, yamlFileName);

        File.Copy(automationFrameworkYamlPath, yamlFileCopyPath, overwrite: true);

        await PrepareYamlAsync(yamlFileCopyPath, modifyYaml);

        // Explanation on the CLI arguments used below:
        // - --add-host and --network host: Lets us connect to the host OS's localhost, where the OC app runs, with
        //   https://localhost. See https://stackoverflow.com/a/24326540/220230. --network host serves the same, but
        //   only works under Linux. See https://docs.docker.com/engine/reference/commandline/run/#network and
        //   https://docs.docker.com/network/drivers/host/.
        // - --volume: Mounts the given host folder as a volume under the given container path. This is to pass files
        //   back and forth between the host and the container.
        // - --tty: Allocates a pseudo-teletypewriter, i.e. redirects the output of ZAP to the CLI's output.
        // - zap.sh: The entry point of ZAP. Everything that comes after this is executed in the container.

        // Also see https://www.zaproxy.org/docs/docker/about/#automation-framework.

        // Running a ZAP desktop in the browser with Webswing with the same config under Windows:
#pragma warning disable S103 // Lines should not be too long
        // docker run --add-host localhost:host-gateway -u zap -p 8080:8080 -p 8090:8090 -i softwaresecurityproject/zap-weekly:20231113 zap-webswing.sh
#pragma warning restore S103 // Lines should not be too long

        var cliParameters = new List<object> { "run" };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            cliParameters.Add("--network");
            cliParameters.Add("host");
        }
        else
        {
            cliParameters.Add("--add-host");
            cliParameters.Add("localhost:host-gateway");
        }

        cliParameters.AddRange(new object[]
        {
            "--volume",
            mountedDirectoryPath + ":/zap/wrk/:rw",
            "--tty",
            _zapImage,
            "zap.sh",
            "-cmd",
            "-autorun",
            "/zap/wrk/" + yamlFileName,
        });

        var stdErrBuffer = new StringBuilder();

        var result = await _docker
            .GetCommand(cliParameters)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line => _testOutputHelper.WriteLineTimestampedAndDebug(line)))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            // This is so no exception is thrown by CliWrap if the exit code is not 0.
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(_cancellationTokenSource.Token);
    }

    public ValueTask DisposeAsync()
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_cancellationTokenSource != null) return;

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        try
        {
            await _pullSemaphore.WaitAsync(token);

            if (!_wasPulled)
            {
                await _docker.ExecuteAsync(token, "pull", _zapImage);
                _wasPulled = true;
            }
        }
        finally
        {
            _pullSemaphore.Release();
        }
    }

    private async Task PrepareYamlAsync(string yamlFilePath, Func<object, Task> modifyYaml)
    {
        var originalYaml = await File.ReadAllTextAsync(yamlFilePath, _cancellationTokenSource.Token);

        var deserializer = new DeserializerBuilder().Build();
        // Deseralizing into a free-form object, not to potentially break unknown fields during reserialization.
        var configuration = deserializer.Deserialize<object>(originalYaml);

        if (modifyYaml != null) await modifyYaml(configuration);

        // Serializing the results.
        var serializer = new SerializerBuilder().WithQuotingNecessaryStrings().Build();
        var updatedYaml = serializer.Serialize(configuration);

        await File.WriteAllTextAsync(yamlFilePath, updatedYaml, _cancellationTokenSource.Token);
    }

    private static void SetStartUrlInYaml(object configuration, Uri startUri)
    {
        List<object> contexts = ((dynamic)configuration)["env"]["contexts"];

        if (!contexts.Any())
        {
            throw new ArgumentException("The supplied ZAP Automation Framework YAML file should contain at least one context.");
        }

        var context = (Dictionary<object, object>)contexts[0];

        if (contexts.Count > 1)
        {
            context =
                (Dictionary<object, object>)contexts
                    .Find(context =>
                        ((Dictionary<object, object>)context).TryGetValue("name", out var name) && (string)name == "Default Context") ??
                context;
        }

        // Setting URLs in the context.
        // Setting includePaths in the context is not necessary because by default everything under urls will be scanned.

        if (!context.ContainsKey("urls")) context["urls"] = new List<object>();

        var urls = (List<object>)context["urls"];

        if (urls.Count > 1)
        {
            throw new ArgumentException(
                "The context in the ZAP Automation Framework YAML file should contain at most a single url in the urls section.");
        }

        if (urls.Count == 1) urls.Clear();

        urls.Add(startUri.ToString());
    }
}

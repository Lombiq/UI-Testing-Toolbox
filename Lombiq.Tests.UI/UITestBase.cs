#nullable enable

using Atata;
using Atata.Xunit;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.Services.GitHub;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI;

public abstract class UITestBase : AtataTestSuite
{
    private readonly List<Func<OrchardCoreUITestExecutorConfiguration, Task>> _configurationChain = new(capacity: 1);
    private readonly List<Func<UITestContext, Task>> _testChain = new(capacity: 1);

    protected ITestOutputHelper _testOutputHelper;

    protected abstract WebApplicationInstanceFactory InstanceFactory { get; }

    public Func<UITestContext, Task<Uri>>? Setup { get; set; }

    public Browser Browser { get; set; }

    static UITestBase() => AtataFactory.SetupShellCliCommandFactory();

    protected UITestBase(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

    protected async Task ExecuteOrchardCoreTestAsync(
        WebApplicationInstanceFactory webApplicationInstanceFactory,
        UITestManifest testManifest,
        OrchardCoreUITestExecutorConfiguration configuration)
    {
        foreach (var configureAsync in _configurationChain)
        {
            await configureAsync(configuration);
        }

        var originalTestOutputHelper = _testOutputHelper;

        Action? afterTest = null;
        if (configuration.ExtendGitHubActionsOutput &&
            configuration.GitHubActionsOutputConfiguration.EnablePerTestOutputGrouping &&
            GitHubHelper.IsGitHubEnvironment)
        {
            (_testOutputHelper, afterTest) =
                GitHubActionsGroupingTestOutputHelper.CreateDecorator(_testOutputHelper, testManifest);
            configuration.TestOutputHelper = _testOutputHelper;
        }

        // Used by many utilities to turn off ANSI escape sequences.
        Environment.SetEnvironmentVariable("NO_COLOR", "true");

        try
        {
            await UITestExecutor.ExecuteOrchardCoreTestAsync(
                webApplicationInstanceFactory,
                testManifest,
                configuration);
        }
        finally
        {
            _testOutputHelper = originalTestOutputHelper;
            afterTest?.Invoke();
        }
    }

    protected override void ConfigureTestAtataContext(AtataContextBuilder builder) =>
        _configurationChain.Insert(0, ConfigureAsync);

    protected virtual Task ConfigureAsync(OrchardCoreUITestExecutorConfiguration configuration)
    {
        Configure(configuration);
        return Task.CompletedTask;
    }

    protected virtual void Configure(OrchardCoreUITestExecutorConfiguration configuration)
    {
        // Intentionally empty. Override in descendant class to customize.
    }

    public UITestBase AddConfiguration(Func<OrchardCoreUITestExecutorConfiguration, Task>? configureAsync)
    {
        if (configureAsync != null) _configurationChain.Add(configureAsync);
        return this;
    }

    public UITestBase AddSyncConfiguration(Action<OrchardCoreUITestExecutorConfiguration> configureSync)
    {
        _configurationChain.Add(configureSync.AsCompletedTask());
        return this;
    }

    public UITestBase UseSetup(Func<UITestContext, Task<Uri>> setupAsync)
    {
        Setup = setupAsync;
        return this;
    }

    public UITestBase AddTest(Func<UITestContext, Task> testAsync)
    {
        _testChain.Add(testAsync);
        return this;
    }

    public UITestBase WithoutBrowser()
    {
        Browser = Browser.None;
        return this;
    }

    public Task RunAsync(Func<UITestContext, Task> finalTest) => AddTest(finalTest).RunAsync();

    public virtual Task RunAsync()
    {
        var testManifest = new UITestManifest(this, _testChain);

        var configuration = new OrchardCoreUITestExecutorConfiguration
        {
            OrchardCoreConfiguration = new OrchardCoreConfiguration(),
            TestOutputHelper = _testOutputHelper,
            BrowserConfiguration = { Browser = Browser },
        };

        return ExecuteOrchardCoreTestAsync(InstanceFactory, testManifest, configuration);
    }
}

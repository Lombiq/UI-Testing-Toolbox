using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services.GitHub;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// Suppose you want to write UI tests in Javascript. Why would you want to do that? Unlikely if you are an Orchard Core
// developer, but what if the person responsible for writing the tests is not? In the previous training section we
// discussed using a separate frontend server, with mention of technologies using Node.js. In that case the frontend
// developers may be more familiar with Javascript so it makes sense to write and debug the tests in Node.js so they
// don't have to learn different tools and tech stacks just to create some UI tests.
public class JavascriptTests : UITestBase
{
    public JavascriptTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public Task ExampleJavascriptTestShouldWork()
    {
        // Don't forget to mark the script files as "Copy if newer", so they are available to the
        // test. It's best to include something like the following in your csproj file:
        // <None Update="Tests\*.mjs" CopyToOutputDirectory="PreserveNewest" />
        var scriptPath = Path.Join("Tests", "test.mjs");

        // Set up the JS dependencies in the test's temp directory to ensure there are no clashes, then run the script.
        return ExecuteTestAfterSetupAsync(context => context
            .SetupSeleniumAndExecuteJavascriptTestAsync(scriptPath, _testOutputHelper));
    }

    // To best debug the Javascript code, you may want to set up the site and then invoke node manually. This is not a
    // real test, but it sets up the site in interactive mode (see Tests/InteractiveModeTests.cs for more) with
    // information how to start up test script from your GUI. It's an example of some tooling that can improve the test
    // developer's workflow.
    [Fact]
    public Task Sandbox()
    {
        // This "test" will wait indefinitely, so it's important to skip it in CI.
        if (GitHubHelper.IsGitHubEnvironment) return Task.CompletedTask;

        return ExecuteTestAfterSetupAsync(
            async context =>
            {
                var driverPath = context.GetDriverPath();
                var tempPath = context.GetTempSubDirectoryPath();

                await context.SwitchToInteractiveAsync(
                    $"To start a Javascript test, open a command line terminal at \"{tempPath}\": and type the " +
                    $"following command: <code class=\"d-block\">node --inspect ../../sandbox.js {driverPath} " +
                    $"<a href=\"{context.Driver.Url}\">{context.Driver.Url}</a></code>");
            },
            configuration =>
            {
                // Since this is an interactive "test", make sure the browser is always displayed.
                configuration.BrowserConfiguration.Headless = false;
                return Task.CompletedTask;
            });
    }
}

// END OF TRAINING SECTION: Executing tests written in Javascript.

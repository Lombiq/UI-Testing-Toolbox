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

    // Using this approach you only have to write minimal C# boilerplate, which you can see below.
    [Fact]
    public Task ExampleJavascriptTestShouldWork() =>
        ExecuteTestAfterSetupAsync(context =>
        {
            // Don't forget to mark the script files as "Copy if newer", so they are available to the test. It's best to
            // include something like the following in your csproj file:
            // <None Update="Tests\*.mjs" CopyToOutputDirectory="PreserveNewest" />
            var workingDirectory = "Tests";
            var scriptPath = Path.Join(workingDirectory, "test.mjs");

            // Set up the JS dependencies in the test's temp directory to ensure there are no clashes, then run the
            // script. This method has an additional parameter to list further NPM dependencies beyond
            // "selenium-webdriver", if the script requires it. We will check out this script file in the next station.
            return context.SetupSeleniumAndExecuteJavascriptTestAsync(_testOutputHelper, scriptPath, workingDirectory);
        });

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
                var workingDirectory = "Tests";
                var scriptPath = Path.Join(workingDirectory, "test.mjs");

                await context.SetupNodeSeleniumAsync(_testOutputHelper, workingDirectory);
                await context.SwitchToInteractiveWithJavascriptTestInfoAsync(scriptPath, workingDirectory);
            },
            configuration =>
            {
                // Since this is an interactive "test", make sure the browser is always displayed.
                configuration.BrowserConfiguration.Headless = false;
                return Task.CompletedTask;
            });
    }
}

// NEXT STATION: Head over to Tests/test.mjs.

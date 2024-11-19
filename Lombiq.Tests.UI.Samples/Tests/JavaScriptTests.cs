using Lombiq.Tests.UI.Extensions;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// Let's suppose you want to write UI tests in JavaScript. Why would you want to do that? Unlikely if you are an Orchard
// Core developer, but what if the person responsible for writing the tests is not? In the previous training section we
// discussed using a separate frontend server, with mention of technologies using Node.js. In that case the frontend
// developers may be more familiar with JavaScript so it makes sense to write and debug the tests in Node.js so they
// don't have to learn different tools and tech stacks just to create some UI tests.
public class JavaScriptTests : UITestBase
{
    public JavaScriptTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // Using this approach you only have to write minimal C# boilerplate, which you can see below.
    [Fact]
    public Task ExampleJavaScriptTestShouldWork() =>
        ExecuteTestAfterSetupAsync(context =>
        {
            // Don't forget to mark the script files as "Copy if newer", so they are available to the test. If you
            // include something like the following in your csproj file, then you only have to do this once:
            // <None Update="Tests\*.mjs" CopyToOutputDirectory="PreserveNewest" />
            var scriptPath = Path.Join("Tests", "JavaScriptTests.mjs");

            // Set up the JS dependencies in the test's temp directory to ensure there are no clashes, then run the
            // script. This method has an additional parameter to list further NPM dependencies beyond
            // "selenium-webdriver", if the script requires it. We will check out this script file in the next station.
            return context.SetupSeleniumAndExecuteJavaScriptTestAsync(_testOutputHelper, scriptPath);
        });

    // To best debug the JavaScript code, you may want to set up the site and then invoke node manually. This is not a
    // real test, but it sets up the site in interactive mode (see Tests/InteractiveModeTests.cs for more) with
    // information on how to start up test scripts from your GUI. It's an example of some tooling that can improve the
    // test developer's workflow.
    [Fact]
    public Task Sandbox() =>
        OpenSandboxAfterSetupAsync(async context =>
        {
            await context.SetupNodeSeleniumAsync(_testOutputHelper);
            await context.SwitchToInteractiveWithJavaScriptTestInfoAsync(Path.Join("Tests", "JavaScriptTests.mjs"));
        });
}

// NEXT STATION: Head over to Tests/JavaScriptTests.mjs.

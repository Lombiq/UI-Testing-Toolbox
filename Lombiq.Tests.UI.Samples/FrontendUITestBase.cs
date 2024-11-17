using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples;

// Sometimes Orchard Core is used in a headless manner, as a web API server that the visitors reach through a web
// frontend (for example a Vue or React single page application). In this scenario you can test the API directly and
// test the frontend with a dummy backend separately, but that will only get you so far. You'll want some tests that
// ensure the two work together. To make this happen, you need some custom logic to initialize the frontend process
// after setup, with a unique port number to avoid clashes. Also the frontend may need the backend URL, whose port is
// already randomized with every test.
public abstract class FrontendUITestBase : UITestBase
{
    protected FrontendUITestBase(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    /// <summary>
    /// Executes a UI test where the frontend is served by a separate process.
    /// </summary>
    [SuppressMessage("Style", "IDE0055:Fix formatting", Justification = "Needed for more readable comments.")]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1114:Parameter list should follow declaration", Justification = "Same.")]
    protected Task ExecuteFrontendTestAfterSetupAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync = null) =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                // Before executing provided test, we switch to the frontend URL, as if we switched to a different
                // tenant, then actually navigate to the frontend so the tests won't start in the backend home page
                // that's probably not used anyway.
                context.SwitchToFrontend();
                await context.GoToHomePageAsync();

                await testAsync(context);
            },
            browser,
            configuration =>
            {
                // The FrontendServer instance manages the test configuration by registering event handlers for the
                // application startup and stop. It also initializes the default frontend and backend URLs. Below the
                // "name" is the label you will find in the test application logs in front of all lines that come from
                // this process. The process's arguments can be set with the arguments array, but likely you'll want to
                // set pass in the frontend and backend ports too which you can do in the "configureCommand" function.
                new FrontendServer(name: "http-server (test frontend)", configuration, _testOutputHelper)
                    .Configure(
                        // Here we use NPX which executes an NPM package without locally installing it, to avoid having
                        // to maintain a separate binary or script just for this sample.
                        program: "npx",
                        arguments: null,
                        // This function lets you edit the command dynamically before the process is created. This is
                        // needed to add the frontend and backend URLs or port numbers to the process, e.g. as arguments
                        // or environment variables. Since we set the arguments here, the parameter above is left null.
                        // If necessary, this is also where you'd set the program's working directory.
                        configureCommand: (frontendCommand, context) =>
                        {
                            // You can also get this from the configuration using configuration
                            // .GetFrontendAndBackendUris().FrontendUri.Port. The values in the configuration have been
                            // initialized shortly before this function is called, but it's not worth it unless you need
                            // both frontend and backend URLS. The frontend port is a unique, reserved number that's
                            // guaranteed to be available during this test just as much as any Orchard Core instance,
                            // because it's coming from the same pool of numbers.
                            var port = context.FrontendPort.ToTechnicalString();

                            // Here we configure NPX to automatically download http-server without prompting (--yes) and
                            // use the provided port number. Since this server uses HTTP instead of HTTPS, you have to
                            // set the frontend URL too. The backend URL is not changed, so pass null to leave it as-is.
                            configuration.SetFrontendAndBackendUris(
                                frontendUrl: $"http://localhost:" + port,
                                backendUrl: null);
                            return frontendCommand.WithArguments(["--yes", "http-server", "--port", port]);
                        },
                        // When this function is not null, test setup will call it on each output line and wait for it
                        // to return true. This can be used to look for an output that only appears when the frontend
                        // server is done with its own startup.
                        checkProgramReady: (line, _) => line?.Contains("Hit CTRL-C to stop the server") == true,
                        // You can use this async function to perform additional tasks right before the FrontendServer's
                        // BeforeAppStart event handler is finished. You can also edit the Orchard Core instance's
                        // startup command here. We don't need it right now.
                        thenAsync: _ => Task.CompletedTask,
                        // Don't start up the server during setup and snapshot restore. You'd rarely want that, so we
                        // have prepared a static method to generate a callback for this parameter.
                        skipStartup: FrontendServer.SkipDuringSetupAndRestore(configuration),
                        // Since we call NPX, the process may start with downloading from the network. So we allow some
                        // grace period here, but it's unlikely that it would take too long unless the program is stuck
                        // or frozen somehow. In that case it's better to fail than wait forever.
                        startupTimeout: TimeSpan.FromMinutes(3));

                return changeConfigurationAsync.InvokeFuncAsync(configuration);
            });
}

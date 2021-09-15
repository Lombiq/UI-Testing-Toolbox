# Executing tests



## Executing tests from Visual Studio

Executing tests from Visual Studio is as simple as running them from [Test Explorer](https://docs.microsoft.com/en-us/visualstudio/test/run-unit-tests-with-test-explorer). Make sure that you follow the docs on [creating test projects](CreatingTests.md) so the tests can show up.


## Executing tests from the command line

In a CI environment you'd execute tests with the `dotnet` command line tool. These are the steps we recommend for CI builds:

1. Build the solution with `dotnet build` in Release mode. We recommend using our [.NET Analyzers](https://github.com/Lombiq/.NET-Analyzers) for static code analysis and applying the code analysis switches on this step and during the `dotnet publish` ones later.
2. Publish the web app's project with `dotnet publish` in Release mode. Note that since the web app shouldn't really reference your UI test projects this doesn't publish those. Remove or don't publish the _refs_ folder. That way, Razor Runtime Compilation will be switched off, which removes an unnecessary and slow step when executing UI tests.
3. Publish the UI test project(s) with `dotnet publish` in Release mode.
4. Run the UI tests with `dotnet test`. Note that by default, the app will run in the Development environment, which is what we need for testing.
5. Optionally, if you want to reuse the build agent, kill the following processes that might remain after UI testing: chromedriver.exe, dotnet.exe, geckodriver.exe, IEDriverServer.exe, msedgedriver.exe.

Also see [what to configure](Configuration.md), especially for multi-agent build machines and tuning parallelization.

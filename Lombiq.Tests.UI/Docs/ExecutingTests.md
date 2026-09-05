# Executing tests

## Executing tests from Visual Studio

Execute tests from [Test Explorer](https://docs.microsoft.com/en-us/visualstudio/test/run-unit-tests-with-test-explorer) in an IDE version with Microsoft Testing Platform support. Follow the docs on [creating test projects](CreatingTests.md) so the tests can show up. See [xUnit's IDE setup instructions](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform) if tests aren't discovered.

## Executing tests from the command line

In a CI environment you'd execute tests with the `dotnet` command line tool. These are the steps we recommend for CI builds:

Use .NET SDK 10 or later and select Microsoft Testing Platform in the solution's _global.json_, preserving any existing SDK settings:

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

1. Build the solution with `dotnet build` in Release mode. We recommend using our [.NET Analyzers](https://github.com/Lombiq/.NET-Analyzers) for static code analysis and applying the code analysis switches on this step and during the `dotnet publish` ones later.
2. Publish the web app's project with `dotnet publish` in Release mode, optionally also with [ReadyToRun](https://docs.microsoft.com/en-us/dotnet/core/deploying/ready-to-run). Note that since the web app shouldn't really reference your UI test projects this doesn't publish those. Remove or don't publish the _refs_ folder. That way, Razor Runtime Compilation will be switched off, which removes an unnecessary and slow step when executing UI tests.
3. Publish the UI test project(s) with `dotnet publish` in Release mode.
4. Run the UI tests with `dotnet test --project path/to/Tests.csproj --configuration Release --no-build`. To run a solution, use `--solution path/to/Solution.slnx`. Note that by default, the app will run in the Development environment, which is what we need for testing.
5. Optionally, if you want to reuse the build agent, kill the following processes that might remain after UI testing (the [_KillLeftoverProcesses.bat_](./KillLeftoverProcesses.bat) script can do this):
   - chromedriver.exe
   - dotnet.exe
   - geckodriver.exe
   - msedgedriver.exe

Also see [what to configure](Configuration.md), especially for multi-agent build machines and tuning parallelization.

Use `--report-trx` to write TRX results and `--report-gh` for GitHub Actions annotations and job summaries. These switches require the corresponding reporting packages, which the Lombiq test SDK includes. The `test-dotnet` action in Lombiq GitHub Actions enables both. xUnit 4 supports existing `--filter` expressions, and _xunit.runner.json_ continues to configure parallelism.

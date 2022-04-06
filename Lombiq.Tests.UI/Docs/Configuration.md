# Configuration



## Configuration from code

All the necessary aspects of the Toolbox can be configured from code. Look for such parameters on various methods. The root configuration class that can be set up for every test individually is `OrchardCoreUITestExecutorConfiguration`.


## External configuration

Note that since the tests are xUnit tests you can configure general parameters of test execution, including the level or parallelization, with [an xUnit configuration file](https://xunit.net/docs/configuration-files) (*xunit.runner.json*). A default suitable one is included in the UI Testing Toolbox and will be loaded into your test projects; if you want to override that then:

1. Add a suitable *xunit.runner.json* file to your project's folder.
2. In the `csproj` configure its "Build Action" as "Content", and "Copy to Output Directory" as "Copy if newer" to ensure it'll be used by the tests. This is how it looks like in the project file:

```xml
<ItemGroup>
  <None Remove="xunit.runner.json" />
  <Content Include="xunit.runner.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

Certain test execution parameters can be configured externally too, the ones retrieved via the `TestConfigurationManager` class. All configuration options are basic key-value pairs and can be provided in one of the two ways:

- Key-value pairs in a *TestConfiguration.json* file. Note that this file needs to be in the folder where the UI tests execute. By default this is the build output folder of the given test project, i.e. where the projects's DLL is generated  (e.g. *bin/Debug/net6.0*).
- Environment variables: Their names should be prefixed with `Lombiq_Tests_UI`, followed by the config with a `__` as it is with [(ASP).NET configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0#environment-variables), e.g. `Lombiq_Tests_UI__OrchardCoreUITestExecutorConfiguration__MaxRetryCount` (instead of the double underscore you can also use a `:` on certain platforms like Windows). Keep in mind that you can set these just for the current session too. Configuration in environment variables will take precedence over the *TestConfiguration.json* file. When you're setting environment variables while trying out test execution keep in mind that you'll have to restart the app after changing any environment variable.

Here's a full *TestConfiguration.json* file example, something appropriate during development when you have a fast machine (probably faster then the one used to execute these tests) and want tests to fail fast instead of being reliable:

```json
{
  "Lombiq_Tests_UI": {
    "AgentIndex": 3,
    "TimeoutConfiguration": {
      "RetryTimeoutSeconds": 5,
      "RetryIntervalMillisecondSeconds": 300,
      "PageLoadTimeoutSeconds": 120
    },
    "OrchardCoreUITestExecutorConfiguration": {
      "MaxRetryCount": 0,
      "RetryIntervalSeconds": 0,
      "MaxRunningConcurrentTests": 0
    },
    "BrowserConfiguration": {
      "Headless": true
    }
  }
}

```

Note that this will execute tests in headless mode, so no browser windows will be opened (for browsers that support it). If you want to troubleshoot a failing test then disable headless mode.

We encourage you to experiment with a `RetryTimeoutSeconds` value suitable for your hardware. Higher, paradoxically, is usually less safe.

`MaxRunningConcurrentTests` sets how many tests should run at the same time. Use a value of `0` to indicate that you would like the default behavior. Use a value of `-1` to indicate that you do not wish to limit the number of tests running at the same time. The default behaviour and `0` uses the [System.Environment.ProcessorCount](https://docs.microsoft.com/en-us/dotnet/api/system.environment.processorcount?view=net-6.0#remarks) property. Set any other positive integer to limit to the exact number.

## <a name="multi-process"></a>Multi-process test execution

UI tests are executed in parallel by default for the given test execution process (see the [xUnit documentation](https://xunit.net/docs/running-tests-in-parallel.html)). However, if you'd like multiple processes to execute tests like when multiple build agents run tests for separate branches on the same build machine then you'll need to tell each process which build agent they are on. This is so clashes on e.g. network port numbers can be prevented.

Supply the agent index in the `AgentIndex` configuration. It doesn't need to but is highly recommended to be zero-indexed (see the [docs on limits](Limits.md)) and it must be unique to each process. You can also use this to find a port interval where on your machine there are no other processes listening.

If you have multiple UI test projects in a single solution and you're executing them with a single `dotnet test` command then disable them being executed in parallel with the xUnit `"parallelizeAssembly": false` configuration (i.e. while tests within a project will be executed in parallel, the two test projects won't, not to have port and other clashes due to the same `AgentIndex`). This is provided by the *xunit.runner.json* file of the UI Testing Toolbox by default. 


## Using SQL Server from a Docker container

### Setup

You can learn more about the *microsoft-mssql-server* container [here](https://hub.docker.com/_/microsoft-mssql-server). You have to mount a local volume that can be shared between the host and the container. Update the values of `device` and `SA_PASSWORD` in the code below and execute it.

```powershell
docker pull mcr.microsoft.com/mssql/server
docker volume create --driver local -o o=bind -o type=none -o device="C:\docker\data" data
docker run --name sql2019 -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=yourStrong(!)Password" -v data:/data -p 1433:1433 -d mcr.microsoft.com/mssql/server:2019-latest
```

Now log in as root using `docker exec -u 0 -it sql2019 bash` and give access to the data directory with `chown 'mssql:root' /data`.

You can use Docker Desktop to stop or start the container going forward. 


### Extending TestConfiguration.json

SQL Server on Linux only has SQL Authentication and you still have to tell the toolbox about your backup paths. Add the following properties to your `Lombiq_Tests_UI`. Adjust the Password field of the connection string and `HostSnapshotPath` property as needed.

```json
"SqlServerDatabaseConfiguration": {
    "ConnectionStringTemplate": "Server=.;Database=LombiqUITestingToolbox_{{id}};User Id=sa;Password=yourStrong(!)Password;MultipleActiveResultSets=True;Connection Timeout=60;ConnectRetryCount=15;ConnectRetryInterval=5"
},
"DockerConfiguration": {
    "ContainerSnapshotPath": "/data",
    "HostSnapshotPath": "C:\\docker\\data"
}
```

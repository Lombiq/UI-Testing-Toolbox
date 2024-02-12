# Configuration

## Configuration from code

All the necessary aspects of the Toolbox can be configured from code. Look for such parameters on various methods. The root configuration class that can be set up for every test individually is `OrchardCoreUITestExecutorConfiguration`.

## External configuration

Note that since the tests are xUnit tests you can configure general parameters of test execution, including the level or parallelization, with [an xUnit configuration file](https://xunit.net/docs/configuration-files) (_xunit.runner.json_). A default suitable one is included in the UI Testing Toolbox and will be loaded into your test projects; if you want to override that then:

1. Add a suitable _xunit.runner.json_ file to your project's folder.
2. In the `csproj` configure its "Build Action" as "Content", and "Copy to Output Directory" as "Copy if newer" to ensure it'll be used by the tests. This is how it looks like in the project file:

```xml
<ItemGroup>
  <None Remove="xunit.runner.json" />
  <Content Include="xunit.runner.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

Note also that some projects' _xunit.runner.json_ files may include the flag [`stopOnFail`](https://xunit.net/docs/configuration-files#stopOnFail) set to `true`, which makes further tests stop once a failing test is encountered.

Certain test execution parameters can be configured externally too, the ones retrieved via the `TestConfigurationManager` class. All configuration options are basic key-value pairs and can be provided in one of the two ways:

- Key-value pairs in a _TestConfiguration.json_ file. Note that this file needs to be in the folder where the UI tests execute. By default this is the build output folder of the given test project, i.e. where the projects's DLL is generated (e.g. _bin/Debug/net6.0_).
- Environment variables: Their names should be prefixed with `Lombiq_Tests_UI`, followed by the config with a `__` as it is with [(ASP).NET configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/), e.g. `Lombiq_Tests_UI__OrchardCoreUITestExecutorConfiguration__MaxRetryCount` (instead of the double underscore you can also use a `:` on certain platforms like Windows). Keep in mind that you can set these just for the current session too. Configuration in environment variables will take precedence over the _TestConfiguration.json_ file. When you're setting environment variables while trying out test execution keep in mind that you'll have to restart the app after changing any environment variable.

Here's a full _TestConfiguration.json_ file example, something appropriate during development when you have a fast machine (probably faster then the one used to execute these tests) and want tests to fail fast instead of being reliable:

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
      "MaxParallelTests": 0
    },
    "BrowserConfiguration": {
      "Headless": true
    }
  }
}
```

Recommendations and notes for such configuration:

- This will execute tests in headless mode, so no browser windows will be opened (for browsers that support it). If you want to troubleshoot a failing test then disable headless mode.
- We encourage you to experiment with a `RetryTimeoutSeconds` value suitable for your hardware. Higher, paradoxically, is usually less safe.
- If you have several UI test projects it can be cumbersome to maintain a _TestConfiguration.json_ file for each. Instead you can set the value of the `LOMBIQ_UI_TESTING_TOOLBOX_SHARED_TEST_CONFIGURATION` environment variable to the absolute path of a central configuration file and then each project will look it up. If you place an individual _TestConfiguration.json_ into a test directory it will still take precedence in case you need special configuration for just that one.
- `MaxParallelTests` sets how many UI tests should run at the same time. It is an important property if you want to run your UI tests in parallel, check out the inline documentation in [`OrchardCoreUITestExecutorConfiguration`](../Services/OrchardCoreUITestExecutorConfiguration.cs).

### HTML validation configuration

If you want to change some HTML validation rules from only a few specific tests, you can create a custom _.htmlvalidate.json_ file (e.g. _TestName.htmlvalidate.json_). For example:

```json
{
  "extends": [
    "html-validate:recommended"
  ],

  "rules": {
    "attribute-boolean-style": "off",
    "element-required-attributes": "off",
    "no-trailing-whitespace": "off",
    "no-inline-style": "off",
    "no-implicit-button-type": "off",
    "wcag/h30": "off",
    "wcag/h32": "off",
    "wcag/h36": "off",
    "wcag/h37": "off",
    "wcag/h67": "off",
    "wcag/h71": "off"
  },

  "root":  true
}
```

Then you can change the configuration to use that:

```cs
 changeConfiguration: configuration => configuration.HtmlValidationConfiguration.HtmlValidationOptions =
                configuration.HtmlValidationConfiguration.HtmlValidationOptions
                    .CloneWith(validationOptions => validationOptions.ConfigPath =
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestName.htmlvalidate.json")));
```

Make sure to also include the `root` attribute and set it to `true` inside the custom _.htmlvalidate.json_ file and include it in the test project like this:

```xml
  <ItemGroup>
    <Content Include="TestName.htmlvalidate.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <PackageCopyToOutput>true</PackageCopyToOutput>
    </Content>
  </ItemGroup>
 ```

## Multi-process test execution

UI tests are executed in parallel by default for the given test execution process (see the [xUnit documentation](https://xunit.net/docs/running-tests-in-parallel.html)). However, if you'd like multiple processes to execute tests like when multiple build agents run tests for separate branches on the same build machine then you'll need to tell each process which build agent they are on. This is so clashes on e.g. network port numbers can be prevented.

Supply the agent index in the `AgentIndex` configuration. It doesn't need to but is highly recommended to be zero-indexed (see the [docs on limits](Limits.md)) and it must be unique to each process. You can also use this to find a port interval where on your machine there are no other processes listening.

If you have multiple UI test projects in a single solution and you're executing them with a single `dotnet test` command then disable them being executed in parallel with the xUnit `"parallelizeAssembly": false` configuration (i.e. while tests within a project will be executed in parallel, the two test projects won't, not to have port and other clashes due to the same `AgentIndex`). This is provided by the _xunit.runner.json_ file of the UI Testing Toolbox by default.

## Using SQL Server from a Docker container

### Setup

You can learn more about the _microsoft-mssql-server_ container [here](https://hub.docker.com/_/microsoft-mssql-server). You have to mount a local volume that can be shared between the host and the container. Update the values of `device` and `SA_PASSWORD` in the code below and execute it.

#### On Windows

```powershell
docker pull mcr.microsoft.com/mssql/server
docker run --name sql2019 -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Password1!" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2019-latest
docker exec -u 0 sql2019 bash -c "mkdir /data; chmod 777 /data -R; chown mssql:root /data"
```

#### On Linux

You need to put the shared directory inside your _$HOME_, in this example _~/.local/docker/mssql/data_:

```shell
docker pull mcr.microsoft.com/mssql/server
docker run --name sql2019 -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password1!' -p 1433:1433 -d 'mcr.microsoft.com/mssql/server:2019-latest'
docker exec -u 0 sql2019 bash -c 'mkdir /data; chmod 777 /data -R; chown mssql:root /data'
```

If you haven't yet, add your user to the `docker` group.

If you get a `PlatformNotSupportedException`, that's a known problem with _Microsoft.Data.SqlClient_ on .Net 5 and above. As a workaround, temporarily set the project's runtime identifier to `linux-x64` - either [on the terminal](https://github.com/dotnet/SqlClient/issues/1423#issuecomment-1093430430), or by adding `<RuntimeIdentifier>linux-x64</RuntimeIdentifier>` to the project file.

#### On Both

If you want to test it out, type `docker exec -u 0 -it sql2019 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Password1!'` to access the SQL console.

You can use [Docker Desktop](https://www.docker.com/products/docker-desktop/) or [Portainer](https://www.portainer.io) to stop or start the container going forward.

### Extending TestConfiguration.json

SQL Server on Linux only has SQL Authentication and you still have to tell the toolbox about your backup paths. Add the following properties to your `Lombiq_Tests_UI`. Adjust the Password field of the connection string and `HostSnapshotPath` property as needed.

```json
{
  "SqlServerDatabaseConfiguration": {
    "ConnectionStringTemplate": "Server=.;Database=LombiqUITestingToolbox_{{id}};User Id=sa;Password=Password1!;Connection Timeout=60;ConnectRetryCount=15;ConnectRetryInterval=5;TrustServerCertificate=True;Encrypt=false"
  },
  "DockerConfiguration": {
    "ContainerSnapshotPath": "/data/Snapshots",
    "ContainerName": "sql2019"
  }
}
```

Note that `TrustServerCertificate=true;Encrypt=false` is used in the connection string due to breaking changes in _Microsoft.Data.SqlClient_ as described in [this issue](https://github.com/dotnet/SqlClient/issues/1479). The same is also present in the default value of the connection string template. This configuration would be a security hole in production environment, but it's safe for testing and development.

The default value of `ContainerSnapshotPath` is `"/data/Snapshots"` so you can omit that.

## Using PostgreSQL and MySQL (or MariaDB)

While the snapshot functionality is only supported for SQLite and SQL Server databases, you can still run tests that connect to a PostgreSQL or MySQL (MariaDB) database.

This doesn't need any special care, just configure the suitable connection strings when running the Orchard setup. If you're running multiple tests with a single DB then also take care of setting the database table prefix there to a value unique to the current execution of the given test. Note that due to the lack of snapshot support for these DBs, you won't be able to use `ExecuteTestAfterSetupAsync()` from your tests.

For an example of how you can set up a test suite to run tests with all DB engines supported by Orchard (in GitHub Actions), see [this pull request](https://github.com/OrchardCMS/OrchardCore/pull/11194/files).

# Configuration



## Configuration from code

All the necessary aspects of the Toolbox can be configured from code. Look for such parameters on various methods. The root configuration class that can be set up for every test individually is `OrchardCoreUITestExecutorConfiguration`.


## External configuration

Certain test execution parameters can be configured externally too, the ones retrieved via the `TestConfigurationManager` class. All configuration options are basic key-value pairs and can be provided in one of the two ways:

- Environment variables: Their names should be prefixed with `Lombiq.Tests.UI.`. Keep in mind that you can set these just for the current session too.
- Key-value pairs in a *TestConfiguration.json* file. Keys here don't need to be prefixed (since they're not in a global namespace). Configuration here will take precedence. Note that this file needs to be in the folder where the UI tests execute. By default this is the build output folder of the given test project, i.e. where the projects's DLL is generated.

Here's a full *TestConfiguration.json* file example, something appropriate during development when you have a fast machine (probably faster then the one used to execute these tests) and want tests to fail fast instead of being reliable:

```
{
  "TimeoutConfiguration.RetryTimeoutSeconds": 5,
  "TimeoutConfiguration.RetryIntervalMillisecondSeconds": 300,
  "TimeoutConfiguration.PageLoadTimeoutSeconds": 120,
  "OrchardCoreUITestExecutorConfiguration.MaxTryCount": 1,
  "AgentIndex":  3
}
```


## <a name="multi-process"></a>Multi-process test execution

UI tests are executed in parallel by default for the given test execution process. However, if you'd like multiple processes to execute tests like when multiple build agents run tests for separate branches on the same build machine then you'll need to tell each process which build agent they are on. This is so clashes on e.g. network port numbers can be prevented. Supply the agent index (it doesn't need to be zero-indexed but it must be unique to each process) in the `AgentIndex` configuration. You can also use this to find a port interval where on your machine there are no other processes listening.

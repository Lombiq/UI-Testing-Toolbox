# Migration guide

## Migrating from v3.*.*

### Preparing WebApplication

1. Prepare WebApplication as described in [Basic tests with the default WebApplicationFactory](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?source=recommendations&view=aspnetcore-6.0#basic-tests-with-the-default-webapplicationfactory).
2. Add `Microsoft.Extensions.Configuration.ConfigurationManager` instance directly to `WebApplicationBuilder.Services`.

```diff
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseNLogHost();

var configuration = builder.Configuration;

- builder.Services.AddOrchardCms(orchard => orchard.ConfigureUITesting(configuration, enableShortcutsDuringUITesting: true));
+ builder.Services.Add(new ServiceDescriptor(configuration.GetType(), configuration));
+ builder.Services.AddOrchardCms();

var app = builder.Build();

app.UseStaticFiles();
app.UseOrchardCore();
app.Run();

+ // As described here(https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-6.0).
+ #pragma warning disable CA1050
+ public partial class Program
+ #pragma warning restore CA1050
+ {
+     protected Program()
+     {
+         // Nothing to do here.
+     }
+ }
```

### Preparing UI test project

1. Add WebApplication project as a `Project reference` to UI test project.

```diff
  </ItemGroup>

  <ItemGroup>
+    <ProjectReference Include="..\..\src\Lombiq.OSOCE.Web\Lombiq.OSOCE.Web.csproj" />
    <ProjectReference Include="..\..\src\Modules\Lombiq.ChartJs\Lombiq.ChartJs.Tests.UI\Lombiq.ChartJs.Tests.UI.csproj" />
    <ProjectReference Include="..\..\src\Modules\Lombiq.DataTables\Lombiq.DataTables\Tests\Lombiq.DataTables.Tests.UI\Lombiq.DataTables.Tests.UI.csproj" />
    <ProjectReference Include="..\..\src\Modules\Lombiq.HelpfulExtensions\Lombiq.HelpfulExtensions.Tests.UI\Lombiq.HelpfulExtensions.Tests.UI.csproj" />
```

2. Change `OrchardCoreUITestBase` implementation like below. `AppAssemblyPath` is not required anymore.

```diff
namespace Lombiq.OSOCE.Tests.UI;

- public class UITestBase : OrchardCoreUITestBase
+ public class UITestBase : OrchardCoreUITestBase<Program>
{
-     protected override string AppAssemblyPath => WebAppConfigHelper
-         .GetAbsoluteApplicationAssemblyPath("Lombiq.OSOCE.Web", "net6.0");
```

3. There is a breaking change in adding command line arguments to the WebApplication.

    To add command line argument with value, use `InstanceCommandLineArgumentsBuilder.AddWithValue` instead of double call `ArgumentsBuilder.Add`.

    To add command line switch, use `InstanceCommandLineArgumentsBuilder.AddSwitch`.

```diff
                configuration.HtmlValidationConfiguration.RunHtmlValidationAssertionOnAllPageChanges = false;
                configuration.OrchardCoreConfiguration.BeforeAppStart += (_, argsBuilder) =>
                {
-                     argsBuilder.Add("--OrchardCore:OrchardCore_Admin:AdminUrlPrefix").Add("custom-admin");
+                     argsBuilder.AddWithValue("OrchardCore:OrchardCore_Admin:AdminUrlPrefix", "custom-admin");

                    return Task.CompletedTask;
                };
```

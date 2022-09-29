# Migration guide

## Migrating from v3.*

### Preparing WebApplication

1. Prepare the web app to be tested as described in [Basic tests with the default WebApplicationFactory](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?source=recommendations&view=aspnetcore-6.0#basic-tests-with-the-default-webapplicationfactory).
2. Remove `Lombiq.Tests.UI.AppExtensions` project reference or NuGet package from the web app.
3. Add `Microsoft.Extensions.Configuration.ConfigurationManager` instance directly to `WebApplicationBuilder.Services`.

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

+ // As described here (https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-6.0).
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

1. Add a _project reference_ of the web app to be tested to the UI test project.

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

### Breaking changes

There is a breaking change in adding command line arguments to the WebApplication.

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

### Non breaking changes

#### `Lombiq.Tests.UI.Extensions.ShortcutsUITestContextExtensions`

The following extension methods behave largely the same and their signatures didn't change. Using `WebApplicationFactory` made it possible to use OC services directly instead of using _controller actions_ invoked via browser navigation.

This means that calling the extension methods below don't cause browser navigation any more.

- `SetUserRegistrationTypeAsync`
- `CreateUserAsync`
- `AddUserToRoleAsync`
- `AddPermissionToRoleAsync`
- `EnableFeatureDirectlyAsync`
- `DisableFeatureDirectlyAsync`
- `ExecuteRecipeDirectlyAsync`
- `SelectThemeAsync`
- `GenerateHttpEventUrlAsync`

_Here is a sample for better understanding_:
```diff
    public static async Task EnablePrivacyConsentBannerFeatureAndAcceptPrivacyConsentAsync(this UITestContext context)
    {
        await context.EnablePrivacyConsentBannerFeatureAsync();
-         await context.GoToHomePageAsync();
+         await context.GoToHomePageAsync(onlyIfNotAlreadyThere: false);
        await context.AcceptPrivacyConsentAsync();
        context.Refresh();
    }
```

The original code with the new behavior resulted in a failing test, because the browser pointed to the `Home page` before `await context.EnablePrivacyConsentBannerFeatureAsync()(=> context.EnableFeatureDirectlyAsync({featureId})`, the `context.EnableFeatureDirectlyAsync` not navigates away, so finaly the `await context.GoToHomePageAsync()` call do nothing and the consent banner din't come up.

The solution, in this case, is to call `await context.GoToHomePageAsync(onlyIfNotAlreadyThere: false)`, this result a reload, and the consent banner come up.

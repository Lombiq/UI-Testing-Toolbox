using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Models;
using OrchardCore.Modules;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

public class TenantsController : Controller
{
    private readonly IClock _clock;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly IShellHost _shellHost;
    private readonly IShellSettingsManager _shellSettingsManager;

    public TenantsController(
        IClock clock,
        IDataProtectionProvider dataProtectionProvider,
        IShellHost shellHost,
        IShellSettingsManager shellSettingsManager)
    {
        _clock = clock;
        _dataProtectionProvider = dataProtectionProvider;
        _shellHost = shellHost;
        _shellSettingsManager = shellSettingsManager;
    }

    public async Task<IActionResult> Create(string name, string urlPrefix, string recipe, string connectionString = "", string databaseProvider = "Sqlite")
    {
        if (_shellHost.TryGetSettings(name, out _)) throw new InvalidOperationException("The tenant already exists.");

        // Creates a default shell settings based on the configuration.
        var shellSettings = _shellSettingsManager.CreateDefaultSettings();

        shellSettings.Name = name;
        shellSettings.RequestUrlHost = string.Empty;
        shellSettings.RequestUrlPrefix = urlPrefix;
        shellSettings.State = TenantState.Uninitialized;

        shellSettings["ConnectionString"] = connectionString;
        shellSettings["TablePrefix"] = string.Empty;
        shellSettings["DatabaseProvider"] = databaseProvider;
        shellSettings["Secret"] = Guid.NewGuid().ToString();
        shellSettings["RecipeName"] = recipe;

        await _shellHost.UpdateShellSettingsAsync(shellSettings);

        // Based on OrchardCore.Tenants.Controllers.ApiController.Create.CreateSetupToken.
        var dataProtector = _dataProtectionProvider.CreateProtector("Tokens").ToTimeLimitedDataProtector();
        var token = dataProtector.Protect(shellSettings["Secret"], _clock.UtcNow.Add(new TimeSpan(24, 0, 0)));

        return Redirect($"~/{urlPrefix}?token={token}");
    }
}

using Lombiq.HelpfulLibraries.OrchardCore.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Models;
using OrchardCore.Modules;
using OrchardCore.Mvc.ModelBinding;
using OrchardCore.Tenants;
using OrchardCore.Tenants.Models;
using OrchardCore.Tenants.Services;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[Route("api/tenants2")]
[ApiController]
[Authorize(AuthenticationSchemes = "Api"), IgnoreAntiforgeryToken, AllowAnonymous]
public class TenantCreateApiProxyController : Controller
{
    private readonly IShellHost _shellHost;
    private readonly ShellSettings _currentShellSettings;
    private readonly IAuthorizationService _authorizationService;
    private readonly IShellSettingsManager _shellSettingsManager;
    private readonly IDataProtectionProvider _dataProtectorProvider;
    private readonly IClock _clock;
    private readonly ITenantValidator _tenantValidator;
    private readonly ILogger<TenantCreateApiProxyController> _logger;

    public TenantCreateApiProxyController(
        IShellHost shellHost,
        ShellSettings currentShellSettings,
        IAuthorizationService authorizationService,
        IShellSettingsManager shellSettingsManager,
        IDataProtectionProvider dataProtectorProvider,
        ITenantValidator tenantValidator,
        IOrchardServices<TenantCreateApiProxyController> services)
    {
        _shellHost = shellHost;
        _currentShellSettings = currentShellSettings;
        _authorizationService = authorizationService;
        _dataProtectorProvider = dataProtectorProvider;
        _shellSettingsManager = shellSettingsManager;
        _clock = services.Clock.Value;
        _tenantValidator = tenantValidator;
        _logger = services.Logger.Value;
    }

    [HttpPost]
    [Route("create")]
    [SuppressMessage(
        "Security",
        "SCS0016:Controller method is potentially vulnerable to Cross Site Request Forgery (CSRF).",
        Justification = "It's not handled in the original either.")]
    public async Task<IActionResult> Create(TenantApiModel model)
    {
        try
        {
            return await CreateAsync(model);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to Create!");
            throw;
        }
    }

    private async Task<IActionResult> CreateAsync(TenantApiModel model)
    {
        if (!_currentShellSettings.IsDefaultShell())
        {
            return Forbid();
        }

        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageTenants))
        {
            return this.ChallengeOrForbid("Api");
        }

        await ValidateModelAsync(model, isNewTenant: !_shellHost.TryGetSettings(model.Name, out var settings));

        if (ModelState.IsValid)
        {
            if (model.IsNewTenant)
            {
                // Creates a default shell settings based on the configuration.
                var shellSettings = _shellSettingsManager.CreateDefaultSettings();

                shellSettings.Name = model.Name;
                shellSettings.RequestUrlHost = model.RequestUrlHost;
                shellSettings.RequestUrlPrefix = model.RequestUrlPrefix;
                shellSettings.State = TenantState.Uninitialized;

                shellSettings["Category"] = model.Category;
                shellSettings["Description"] = model.Description;
                shellSettings["ConnectionString"] = model.ConnectionString;
                shellSettings["TablePrefix"] = model.TablePrefix;
                shellSettings["Schema"] = model.Schema;
                shellSettings["DatabaseProvider"] = model.DatabaseProvider;
                shellSettings["Secret"] = Guid.NewGuid().ToString();
                shellSettings["RecipeName"] = model.RecipeName;
                shellSettings["FeatureProfile"] = string.Join(',', model.FeatureProfiles ?? Array.Empty<string>());

                await _shellHost.UpdateShellSettingsAsync(shellSettings);

                var token = CreateSetupToken(shellSettings);

                return Ok(GetEncodedUrl(shellSettings, token));
            }
            else
            {
                // Site already exists, return 201.

                var token = CreateSetupToken(settings);

                return Created(GetEncodedUrl(settings, token), value: null);
            }
        }

        return BadRequest(ModelState);
    }

    private string GetEncodedUrl(ShellSettings shellSettings, string token)
    {
        var host = shellSettings.RequestUrlHosts.FirstOrDefault();
        var hostString = host != null ? new HostString(host) : Request.Host;

        var pathString = HttpContext.Features.Get<ShellContextFeature>()?.OriginalPathBase ?? PathString.Empty;
        if (!string.IsNullOrEmpty(shellSettings.RequestUrlPrefix))
        {
            pathString = pathString.Add('/' + shellSettings.RequestUrlPrefix);
        }

        var queryString = QueryString.Empty;
        if (!string.IsNullOrEmpty(token))
        {
            queryString = QueryString.Create("token", token);
        }

        return $"{Request.Scheme}://{hostString + pathString + queryString}";
    }

    private string CreateSetupToken(ShellSettings shellSettings)
    {
        // Create a public url to setup the new tenant
        var dataProtector = _dataProtectorProvider.CreateProtector("Tokens").ToTimeLimitedDataProtector();
        var token = dataProtector.Protect(
            shellSettings["Secret"],
            new DateTimeOffset(_clock.UtcNow.Add(new TimeSpan(24, 0, 0))));

        return token;
    }

    private async Task ValidateModelAsync(TenantApiModel model, bool isNewTenant)
    {
        model.IsNewTenant = isNewTenant;

        ModelState.AddModelErrors(await _tenantValidator.ValidateAsync(model));
    }
}

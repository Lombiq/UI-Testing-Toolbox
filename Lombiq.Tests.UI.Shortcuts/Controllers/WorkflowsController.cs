using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Modules;
using OrchardCore.Workflows.Http.Controllers;
using OrchardCore.Workflows.Http.Models;
using OrchardCore.Workflows.Services;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[Feature(ShortcutsFeatureIds.Workflows)]
[DevelopmentAndLocalhostOnly]
[AllowAnonymous]
public class WorkflowsController : Controller
{
    private readonly ISecurityTokenService _securityTokenService;
    private readonly IWorkflowTypeStore _workflowTypeStore;

    public WorkflowsController(ISecurityTokenService securityTokenService, IWorkflowTypeStore workflowTypeStore)
    {
        _securityTokenService = securityTokenService;
        _workflowTypeStore = workflowTypeStore;
    }

    public async Task<IActionResult> GenerateHttpEventUrl(string workflowTypeId, string activityId, int tokenLifeSpan)
    {
        var workflowType = await _workflowTypeStore.GetAsync(workflowTypeId);
        if (workflowType == null)
        {
            return NotFound();
        }

        var token = _securityTokenService.CreateToken(
            new WorkflowPayload(workflowType.WorkflowTypeId, activityId),
            TimeSpan.FromDays(
                tokenLifeSpan == 0 ? HttpWorkflowController.NoExpiryTokenLifespan : tokenLifeSpan));

        // LinkGenerator.GetPathByAction(...) and UrlHelper.Action(...) not resolves url for
        // HttpWorkflowController.Invoke action.
        // https://github.com/OrchardCMS/OrchardCore/issues/11764.
        return Ok($"/workflows/Invoke?token={Uri.EscapeDataString(token)}");
    }
}

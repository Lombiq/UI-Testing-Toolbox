using Lombiq.HelpfulLibraries.Libraries.Mvc;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Recipes.Services;
using OrchardCore.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers
{
    [DevelopmentAndLocalhostOnly]
    public class RecipeController : Controller
    {
        private readonly IRecipeExecutor _recipeExecutor;
        private readonly IEnumerable<IRecipeHarvester> _recipeHarvesters;
        private readonly ISiteService _siteService;

        public RecipeController(
            IRecipeExecutor recipeExecutor,
            IEnumerable<IRecipeHarvester> recipeHarvesters,
            ISiteService siteService)
        {
            _recipeExecutor = recipeExecutor;
            _recipeHarvesters = recipeHarvesters;
            _siteService = siteService;
        }

        public async Task<ActionResult> Execute(string recipeName)
        {
            var recipeCollections = await _recipeHarvesters
                .AwaitEachAsync(harvester => harvester.HarvestRecipesAsync());
            var recipe = recipeCollections
                .SelectMany(recipeCollection => recipeCollection)
                .SingleOrDefault(recipeDescriptor => recipeDescriptor.Name == recipeName);
            if (recipe == null) return NotFound();

            var site = await _siteService.GetSiteSettingsAsync();
            var executionId = Guid.NewGuid().ToString("n");

            // This is how Orchard Core did it up until 2021-02-23 including the required version used by this project.
            // Since then they've switched over to IRecipeEnvironmentProvider for environment building. This code should
            // be updated accordingly during Orchard upgrade.
            var environment = new Dictionary<string, object>
            {
                { "SiteName", site.SiteName },
                { "AdminUsername", User.Identity.Name },
                { "AdminUserId", User.FindFirstValue(ClaimTypes.NameIdentifier) },
            };

            await _recipeExecutor.ExecuteAsync(executionId, recipe, environment, default);
            return Ok();
        }
    }
}

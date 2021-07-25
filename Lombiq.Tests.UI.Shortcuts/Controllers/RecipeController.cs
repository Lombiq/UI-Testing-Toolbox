using Lombiq.HelpfulLibraries.Libraries.Mvc;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Recipes.Services;
using OrchardCore.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
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
                .SingleOrDefault(recipeDescriptor => recipeDescriptor.RecipeFileInfo.Name == recipeName);
            if (recipe == null) return NotFound();

            var site = await _siteService.GetSiteSettingsAsync();
            await _recipeExecutor.ExecuteAsync(
                Guid.NewGuid().ToString("N"),
                recipe,
                new
                {
                    site.SiteName,
                    AdminUsername = site.SuperUser,
                },
                default);

            return Ok();
        }
    }
}

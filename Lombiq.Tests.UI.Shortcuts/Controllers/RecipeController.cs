using Lombiq.HelpfulLibraries.Libraries.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OrchardCore.Modules;
using OrchardCore.Recipes.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers
{
    [DevelopmentAndLocalhostOnly]
    public class RecipeController : Controller
    {
        private readonly IRecipeExecutor _recipeExecutor;
        private readonly IEnumerable<IRecipeHarvester> _recipeHarvesters;
        private readonly IEnumerable<IRecipeEnvironmentProvider> _recipeEnvironmentProviders;
        private readonly ILogger _logger;

        public RecipeController(
            IRecipeExecutor recipeExecutor,
            IEnumerable<IRecipeHarvester> recipeHarvesters,
            IEnumerable<IRecipeEnvironmentProvider> recipeEnvironmentProviders,
            ILogger<RecipeController> logger)
        {
            _recipeExecutor = recipeExecutor;
            _recipeHarvesters = recipeHarvesters;
            _recipeEnvironmentProviders = recipeEnvironmentProviders;
            _logger = logger;
        }

        public async Task<ActionResult> Execute(string recipeName)
        {
            var recipeCollections = await _recipeHarvesters
                .AwaitEachAsync(harvester => harvester.HarvestRecipesAsync());
            var recipe = recipeCollections
                .SelectMany(recipeCollection => recipeCollection)
                .SingleOrDefault(recipeDescriptor => recipeDescriptor.Name == recipeName);

            if (recipe == null) return NotFound();

            // Logic copied from OrchardCore.Recipes.Controllers.AdminController.
            var executionId = Guid.NewGuid().ToString("n");

            var environment = new Dictionary<string, object>();
            await _recipeEnvironmentProviders
                .OrderBy(environmentProvider => environmentProvider.Order)
                .InvokeAsync((provider, env) => provider.PopulateEnvironmentAsync(env), environment, _logger);

            await _recipeExecutor.ExecuteAsync(executionId, recipe, environment, CancellationToken.None);
            return Ok();
        }
    }
}

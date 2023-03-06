using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrchardCore.Environment.Extensions;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services.OrchardCoreHosting;

public class ApplicationRecipeHarvesterAsync : RecipeHarvesterAsync
{
    public ApplicationRecipeHarvesterAsync(
        IRecipeReader recipeReader,
        IExtensionManager extensionManager,
        IHostEnvironment hostingEnvironment,
        ILogger<RecipeHarvesterAsync> logger)
        : base(recipeReader, extensionManager, hostingEnvironment, logger)
    {
    }

    public override Task<IEnumerable<RecipeDescriptor>> HarvestRecipesAsync() =>
        HarvestRecipesAsync("Recipes");
}

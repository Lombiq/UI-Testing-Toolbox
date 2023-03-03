using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrchardCore.Environment.Extensions;
using OrchardCore.Modules;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services.OrchardCoreHosting;

public class FakeRecipeHarvester : IRecipeHarvester
{
    private readonly IRecipeReader _recipeReader;
    private readonly IExtensionManager _extensionManager;
    private readonly IHostEnvironment _hostingEnvironment;
    private readonly ILogger _logger;

    public FakeRecipeHarvester(
        IRecipeReader recipeReader,
        IExtensionManager extensionManager,
        IHostEnvironment hostingEnvironment,
        ILogger<FakeRecipeHarvester> logger)
    {
        _recipeReader = recipeReader;
        _extensionManager = extensionManager;
        _hostingEnvironment = hostingEnvironment;
        _logger = logger;
    }

    public virtual Task<IEnumerable<RecipeDescriptor>> HarvestRecipesAsync() =>
        _extensionManager.GetExtensions().InvokeAsync(HarvestRecipesAsync, _logger);

    private Task<IEnumerable<RecipeDescriptor>> HarvestRecipesAsync(IExtensionInfo extension)
    {
        var folderSubPath = PathExtensions.Combine(extension.SubPath, "Recipes");
        return HarvestRecipesAsync(folderSubPath);
    }

    protected async Task<IEnumerable<RecipeDescriptor>> HarvestRecipesAsync(string path)
    {
        var recipeFiles = _hostingEnvironment.ContentRootFileProvider.GetDirectoryContents(path)
            .Where(info => !info.IsDirectory && info.Name.EndsWith(".recipe.json", StringComparison.Ordinal))
            .ToAsyncEnumerable();

        return await recipeFiles.SelectAwait(
            async recipeFile => await _recipeReader.GetRecipeDescriptor(
                path,
                recipeFile,
                _hostingEnvironment.ContentRootFileProvider))
            .ToListAsync();
    }
}

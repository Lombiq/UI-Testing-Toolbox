using Lombiq.HelpfulLibraries.OrchardCore.Liquid;
using Microsoft.Extensions.Configuration;
using OrchardCore.Liquid;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.AppExtensions.PropertyRegistrars;

public class IsUITestingLiquidPropertyRegistrar : ILiquidPropertyRegistrar
{
    private readonly IConfiguration _configuration;
    public IsUITestingLiquidPropertyRegistrar(IConfiguration configuration) =>
        _configuration = configuration;

    public string PropertyName => "is_ui_testing";
    public Task<object> GetObjectAsync(LiquidTemplateContext context)
    {
        var isUiTesting = _configuration.IsUITesting();
        return Task.FromResult<object>(isUiTesting);
    }
}

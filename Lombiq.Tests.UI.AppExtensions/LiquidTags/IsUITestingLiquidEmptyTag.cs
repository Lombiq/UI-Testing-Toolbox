using Fluid;
using Fluid.Ast;
using Lombiq.HelpfulLibraries.OrchardCore.Liquid;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.AppExtensions.LiquidTags;

public class IsUITestingLiquidEmptyTag : ILiquidParserTag
{
    private readonly IConfiguration _configuration;

    public IsUITestingLiquidEmptyTag(IConfiguration configuration) =>
        _configuration = configuration;

    public async ValueTask<Completion> WriteToAsync(
        IReadOnlyList<FilterArgument> argumentsList,
        TextWriter writer,
        TextEncoder encoder,
        TemplateContext context)
    {
        var isUiTesting = _configuration.IsUITesting();

        await writer.WriteAsync(isUiTesting ? "true" : "false");
        return Completion.Normal;
    }
}

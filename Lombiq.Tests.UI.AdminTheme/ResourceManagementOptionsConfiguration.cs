using Lombiq.HelpfulLibraries.OrchardCore.ResourceManagement;
using Lombiq.Tests.UI.AdminTheme.Constants;

namespace Lombiq.Tests.UI.AdminTheme;

public class ResourceManagementOptionsConfiguration : ResourceManagementOptionsConfiguratorBase
{
    protected override string Area => FeatureIds.Area;

    protected override void Configure(ResourceManagementContext context) =>
        context.DefineStyle(ResourceNames.General, "general.css");
}

using OrchardCore.Modules.Manifest;
using static Lombiq.Tests.UI.Shortcuts.ShortcutsFeatureIds;

[assembly: Module(
    Name = "Shortcuts - Lombiq UI Testing Toolbox",
    Author = "Lombiq Technologies",
    Website = "https://github.com/Lombiq/UI-Testing-Toolbox",
    Version = "0.0.1"
)]

[assembly: Feature(
    Id = Default,
    Name = "Lombiq UI Testing Toolbox - Shortcuts",
    Category = "Development",
    Description = DescriptionUiTestWarning + "Provides shortcuts for common operations that UI tests might want to do or check.",
    Dependencies =
    [
        "OrchardCore.ContentManagement",
        "OrchardCore.ContentTypes",
        "OrchardCore.DisplayManagement",
        "OrchardCore.Roles",
        "OrchardCore.Tenants",
        "OrchardCore.Users",
    ]
)]

[assembly: Feature(
    Id = Deployment,
    Name = "Deployment - Shortcuts - Lombiq UI Testing Toolbox",
    Category = "Development",
    Description = "Adds deployment-related features such as recipe steps, that have UI testing specific behavior."
)]

[assembly: Feature(
    Id = FeatureToggleTestBench,
    Name = "Feature Toggle Test Bench - Shortcuts - Lombiq UI Testing Toolbox",
    Category = "Development",
    Description = "WARNING: Don't enable this feature by hand. Can be turned on and off to test if feature state changes work."
)]

[assembly: Feature(
    Id = MediaCachePurge,
    Name = "Media Cache Purge - Shortcuts - Lombiq UI Testing Toolbox",
    Category = "Development",
    Description = DescriptionUiTestWarning + "Provides shortcut for Media Cache Purge.",
    Dependencies =
    [
        "OrchardCore.Media.Cache",
    ]
)]

[assembly: Feature(
    Id = Workflows,
    Name = "Workflows - Shortcuts - Lombiq UI Testing Toolbox",
    Category = "Development",
    Description = DescriptionUiTestWarning + "Provides shortcut for Workflows.",
    Dependencies =
    [
        "OrchardCore.Workflows.Http",
    ]
)]

[assembly: Feature(
    Id = Swagger,
    Name = "Swagger - Shortcuts - Lombiq UI Testing Toolbox",
    Category = "Development",
    Description = DescriptionUiTestWarning + "Provides a Swagger endpoint to generate a JSON OpenAPI definition for " +
                  "the web APIs available in the app. Used in security scanning."
)]

[assembly: Feature(
    Id = ShiftTime,
    Name = "Shift Time - Shortcuts - Lombiq UI Testing Toolbox",
    Category = "Development",
    Description = DescriptionUiTestWarning + "Adds a custom IClock implementation where the clock can be shifted by " +
                  "a specified value."
)]

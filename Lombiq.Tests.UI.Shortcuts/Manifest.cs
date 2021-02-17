using OrchardCore.Modules.Manifest;
using static Lombiq.Tests.UI.Shortcuts.ShortcutsFeatureIds;

[assembly: Module(
    Name = "Lombiq UI Testing Toolbox - Shortcuts",
    Author = "Lombiq Technologies",
    Website = "https://github.com/Lombiq/UI-Testing-Toolbox",
    Version = "0.0.1"
)]

[assembly: Feature(
    Id = Default,
    Name = "Lombiq UI Testing Toolbox - Shortcuts",
    Category = "Development",
    Description = "WARNING: Only enable this feature in the UI testing environment. Provides shortcuts for common operations that UI tests might want to do or check.",
    Dependencies = new[]
    {
        "OrchardCore.ContentManagement",
        "OrchardCore.ContentTypes",
        "OrchardCore.DisplayManagement",
        "OrchardCore.Users",
        "OrchardCore.Media",
    }
)]

[assembly: Feature(
    Id = FeatureToggleTestBench,
    Name = "Lombiq UI Testing Toolbox - Shortcuts - Feature Toggle Test Bench",
    Category = "Development",
    Description = "WARNING: Don't enable this feature by hand. Can be turned on and off to test if feature state changes work."
)]

[assembly: Feature(
    Id = AzureCachePurgeController,
    Name = "Lombiq UI Testing Toolbox - Shortcuts - Azure Cache Purge Controller",
    Category = "Development",
    Description = "WARNING: Don't enable this feature by hand. Can be turned on and off to test if feature state changes work.",
    Dependencies = new[]
    {
        "OrchardCore.Media.Cache",
    }
)]

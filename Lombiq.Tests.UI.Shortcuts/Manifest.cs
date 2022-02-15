using OrchardCore.Modules.Manifest;
using static Lombiq.Tests.UI.Shortcuts.ShortcutsFeatureIds;

[assembly: Module(
    Name = "Shortcuts - Lombiq UI Testing Toolbox",
    Author = "Lombiq Technologies",
    Website = "https://github.com/Lombiq/UI-Testing-Toolbox",
    Version = "1.6.0"
)]

[assembly: Feature(
    Id = Default,
    Name = "Lombiq UI Testing Toolbox - Shortcuts",
    Category = "Development",
    Description = "WARNING: Only enable this feature in the UI testing environment. Provides shortcuts for common " +
        "operations that UI tests might want to do or check.",
    Dependencies = new[]
    {
        "OrchardCore.ContentManagement",
        "OrchardCore.ContentTypes",
        "OrchardCore.DisplayManagement",
        "OrchardCore.Users",
    }
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
    Description = "WARNING: Only enable this feature in the UI testing environment. Provides shortcut for Media Cache Purge.",
    Dependencies = new[]
    {
        "OrchardCore.Media.Cache",
    }
)]

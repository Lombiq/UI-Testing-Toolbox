using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Lombiq UI Testing Toolbox - Shortcuts",
    Author = "Lombiq Technologies",
    Website = "https://github.com/Lombiq/UI-Testing-Toolbox",
    Version = "0.0.1"
)]

[assembly: Feature(
    Id = "Lombiq.Tests.UI.Shortcuts",
    Name = "Lombiq UI Testing Toolbox - Shortcuts",
    Category = "Development",
    Description = "WARNING: Only enable this module in the UI testing environment. Provides shortcuts for common operations that UI tests might want to do or check.",
    Dependencies = new[]
    {
        "OrchardCore.ContentManagement",
        "OrchardCore.ContentTypes",
        "OrchardCore.DisplayManagement",
        "OrchardCore.Users",
    }
)]

using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "TestingModule - Lombiq UI Testing Toolbox",
    Author = "Lombiq Technologies",
    Website = "https://github.com/Lombiq/UI-Testing-Toolbox",
    Version = "0.0.1"
)]

[assembly: Feature(
    Id = "Lombiq.Tests.UI.TestingModule",
    Name = "Lombiq UI Testing Toolbox - TestingModules",
    Category = "Development",
    Description = "WARNING: Only enable this feature in the UI testing environment. Provides shortcuts for common " +
        "operations that UI tests might want to do or check."
)]

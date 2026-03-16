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
    Description = "Contains features that are useful for testing the Testing module itself."
)]

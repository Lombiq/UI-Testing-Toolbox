using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Lombiq UI Testing Toolbox - Testing Module",
    Author = "Lombiq Technologies",
    Website = "https://github.com/Lombiq/UI-Testing-Toolbox",
    Version = "0.0.1"
)]

[assembly: Feature(
    Id = "Lombiq.Tests.UI.TestingModule",
    Name = "Lombiq UI Testing Toolbox - Testing Module",
    Category = "Development",
    Description = "Contains features that are useful for testing the Testing module itself."
)]

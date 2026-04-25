using OrchardCore.DisplayManagement.Manifest;
using OrchardCore.Modules.Manifest;

[assembly: Theme(
    Name = "Lombiq UI Testing Toolbox - Admin Theme",
    Author = "Lombiq Technologies",
    Website = "https://github.com/Lombiq/UI-Testing-Toolbox",
    Version = "0.0.1",
    Description = "Adjustments for the stock Orchard Core admin theme to make it more automation-friendly.",
    BaseTheme = "TheAdmin",
    Dependencies = ["OrchardCore.Themes", "TheAdmin"],
    Tags = [ManifestConstants.AdminTag])]

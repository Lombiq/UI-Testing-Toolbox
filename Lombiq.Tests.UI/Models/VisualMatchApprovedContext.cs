namespace Lombiq.Tests.UI.Models;

public record VisualMatchApprovedContext
{
    public string ModuleName { get; set; }
    public string MethodName { get; set; }
    public string ReferenceFileName { get; set; }
    public string ReferenceResourceName { get; set; }
    public string ModuleDirectory { get; set; }
    public string ReferenceImagePath { get; set; }
}

namespace Lombiq.Tests.UI.Models;

public record VisualVerificationMatchApprovedContext
{
    public string ModuleName { get; set; }
    public string MethodName { get; set; }
    public string BrowserName { get; set; }
    public string BaselineFileName { get; set; }
    public string BaselineResourceName { get; set; }
    public string ModuleDirectory { get; set; }
    public string BaselineImagePath { get; set; }
}

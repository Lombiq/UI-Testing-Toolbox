using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Lombiq.Tests.UI.Models;

public class VisualVerificationMatchApprovedContext
{
    public string ModuleName { get; }
    public string MethodName { get; }
    public string BrowserName { get; }
    public string BaselineImageFileName { get; }
    public string BaselineImageResourceName { get; }

    /// <summary>
    /// Gets the local file system path of the baseline image. Will be <see langword="null"/> if the test is not loaded
    /// from source but binaries; load the image from embedded resources then instead, see <see
    /// cref="BaselineImageResourceName"/>.
    /// </summary>
    public string BaselineImagePath { get; }

    public VisualVerificationMatchApprovedContext(
        UITestContext context,
        VisualVerificationMatchApprovedConfiguration configuration,
        EnhancedStackFrame testFrame)
    {
        ModuleName = GetModuleName(testFrame);
        MethodName = GetMethodName(testFrame);
        BrowserName = context.Driver.As<IHasCapabilities>().Capabilities.GetCapability("browserName") as string;

        BaselineImageFileName = configuration.BaselineFileNameFormatter(configuration, this);
        BaselineImageResourceName = $"{testFrame.MethodInfo.DeclaringType!.Namespace}.{BaselineImageFileName}.png";

        var testSourceDirectoryPath = Path.GetDirectoryName(testFrame.GetFileName());
        if (!string.IsNullOrEmpty(testSourceDirectoryPath))
        {
            BaselineImagePath = Path.Combine(testSourceDirectoryPath!, $"{BaselineImageFileName}.png");
        }
    }

    private static string GetModuleName(EnhancedStackFrame frame)
    {
        var currentMethod = frame.MethodInfo.DeclaringType!;
        string moduleName;

        // This is required to retrieve the class from the inheritance chain where the method was declared but not
        // overridden.
        do
        {
            moduleName = currentMethod.Name;
            currentMethod = currentMethod.DeclaringType;
        }
        while (currentMethod is not null);

        var depthMark = new Regex("^(?<module>.*)`[0-9]+$", RegexOptions.ExplicitCapture);
        if (depthMark.IsMatch(moduleName))
        {
            moduleName = depthMark.Match(moduleName).Groups["module"].Value;
        }

        return moduleName;
    }

    // Retrieves the method name. Removes the decoration in case of if it inherited from a base class but not overridden.
    // Because of the inheritance, the method name gets some decoration in stack trace.
    private static string GetMethodName(EnhancedStackFrame frame)
    {
        var methodName = frame.MethodInfo.Name!;
        var inheritedMethod = new Regex("^<(?<method>.*)>.*$", RegexOptions.ExplicitCapture);
        if (inheritedMethod.IsMatch(methodName))
        {
            methodName = inheritedMethod.Match(methodName).Groups["method"].Value;
        }

        return methodName;
    }
}

using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.Shortcuts.Controllers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.SecurityScanning;

public static class OrchardCoreUITestExecutorConfigurationExtensions
{
    /// <summary>
    /// Sets the <see cref="OrchardCoreUITestExecutorConfiguration.AssertAppLogsAsync"/> to the output of <see
    /// cref="CreateAppLogAssertionForSecurityScan"/> so it accepts errors in the log caused by the security scanning.
    /// </summary>
    public static OrchardCoreUITestExecutorConfiguration UseAssertAppLogsForSecurityScan(
        this OrchardCoreUITestExecutorConfiguration configuration,
        params string[] additionalPermittedErrorLinePatterns)
    {
        configuration.AssertAppLogsAsync = CreateAppLogAssertionForSecurityScan(additionalPermittedErrorLinePatterns);

        return configuration;
    }

    /// <summary>
    /// Similar to <see cref="OrchardCoreUITestExecutorConfiguration.AssertAppLogsCanContainCacheFolderErrorsAsync"/>,
    /// but also permits certain <see cref="LogLevel.Error"/> log messages which represent correct reactions to
    /// incorrect or malicious user behavior during a security scan.
    /// </summary>
    public static Func<IWebApplicationInstance, Task> CreateAppLogAssertionForSecurityScan(params string[] additionalPermittedErrorLinePatterns)
    {
        var permittedErrorLinePatterns = new List<string>
        {
            // The model binding will throw FormatException exception with this text during ZAP active scan, when the
            // bot tries to send malicious query strings or POST data that doesn't fit the types expected by the model.
            // This is correct, safe behavior and should be logged in production.
            "is not a valid value for Boolean",
            "An unhandled exception has occurred while executing the request. System.FormatException: any",
            "System.FormatException: The input string '[\\S\\s]+' was not in a correct format.",
            "System.FormatException: The input string 'any",
            // Happens when the static file middleware tries to access a path that doesn't exist or access a file as a
            // directory. Presumably this is an attempt to access protected files using source path manipulation. This
            // is handled by ASP.NET Core and there is nothing for us to worry about.
            "System.IO.IOException: Not a directory",
            "System.IO.IOException: The filename, directory name, or volume label syntax is incorrect",
            "System.IO.DirectoryNotFoundException: Could not find a part of the path",
            // This happens when a request's model contains a dictionary and a key is missing. While this can be a
            // legitimate application error, during a security scan it's more likely the result of an incomplete
            // artificially constructed request. So the means the ASP.NET Core model binding is working as intended.
            "An unhandled exception has occurred while executing the request. System.ArgumentNullException: Value cannot be null. (Parameter 'key')",
            // One way to verify correct error handling is to navigate to ~/Lombiq.Tests.UI.Shortcuts/Error/Index, which
            // always throws an exception. This also gets logged but it's expected, so it should be ignored.
            ErrorController.ExceptionMessage,
            // Thrown from Microsoft.AspNetCore.Authentication.AuthenticationService.ChallengeAsync() when ZAP sends
            // invalid authentication challenges.
            "System.InvalidOperationException: No authentication handler is registered for the scheme",
        };

        permittedErrorLinePatterns.AddRange(additionalPermittedErrorLinePatterns);

        return app =>
            app.LogsShouldNotContainAsync(logEntry =>
                !permittedErrorLinePatterns.Any(pattern =>
                    Regex.IsMatch(logEntry.ToString(), pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled)) &&
                AppLogAssertionHelper.NotMediaCacheEntries(logEntry));
    }
}

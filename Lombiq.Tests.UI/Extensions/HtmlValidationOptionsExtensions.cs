using System;
using System.IO;

namespace Atata.HtmlValidation;

public static class HtmlValidationOptionsExtensions
{
    /// <summary>
    /// Sets the path of an HTML-validate config file local to the current app as <see
    /// cref="HtmlValidationOptions.ConfigPath"/>.
    /// </summary>
    /// <param name="configFileRelativePath">The relative path to the HTML-validate config file.</param>
    public static void SetLocalConfigFile(this HtmlValidationOptions htmlValidationOptions, string configFileRelativePath) =>
        htmlValidationOptions.ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFileRelativePath);
}

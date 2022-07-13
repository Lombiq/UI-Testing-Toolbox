using Lombiq.Tests.UI.Constants;
using System;
using System.Drawing;
using System.Globalization;

namespace Lombiq.Tests.UI.Services;

public class BrowserConfiguration
{
    public static readonly CultureInfo DefaultAcceptLanguage = new("en-US");

    /// <summary>
    /// Gets or sets the browser to use for the current test.
    /// </summary>
    public Browser Browser { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the test will use the given browser in headless mode, provided that
    /// there's support for it.
    /// </summary>
    public bool Headless { get; set; } = TestConfigurationManager.GetBoolConfiguration("BrowserConfiguration:Headless", defaultValue: true);

    /// <summary>
    /// Gets or sets the action that will be invoked with the browser's options object so you can modify the options as
    /// necessary.
    /// </summary>
    public Action<object> BrowserOptionsConfigurator { get; set; }

    /// <summary>
    /// Gets or sets the culture that'll be used in the browser to set the Accept-Language HTTP header and make
    /// requests. Defaults to <see cref="DefaultAcceptLanguage"/>.
    /// </summary>
    public CultureInfo AcceptLanguage { get; set; } = DefaultAcceptLanguage;

    /// <summary>
    /// Gets or sets a value indicating whether the resolution should be changed to <see
    /// cref="CommonDisplayResolutions.Standard"/> when the setup is loaded.
    /// </summary>
    public Size DefaultBrowserSize { get; set; } = CommonDisplayResolutions.Standard;
}

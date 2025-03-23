using Lombiq.HelpfulLibraries.Common.Utilities;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

// The awkward async-method-that-returns-a-func pattern is used here because the WebDriver factory method required by
// Atata is synchronous but we need async I/O for the initialization before that.
// If this file is renamed or moved, be sure to adjust the regex in the renovate.json5 config file in the root too.
public static class WebDriverFactory
{
    public static Task<Func<ChromeDriver>> CreateChromeDriverAsync(BrowserConfiguration configuration, TimeSpan pageLoadTimeout) =>
        CreateDriverAsync(() => Task.FromResult(() =>
        {
            var chromeConfig = new ChromeConfiguration { Options = new ChromeOptions().SetCommonOptions() };

            chromeConfig.Options.SetLoggingPreference(LogType.Browser, LogLevel.Info);

            // Linux-specific setting, may be necessary for running in containers, see
            // https://developers.google.com/web/tools/puppeteer/troubleshooting#tips for more information.
            chromeConfig.Options.AddArgument("disable-dev-shm-usage");

            // Disables the "self-XSS" warning in dev tools (when you have to type "allow pasting"), see
            // https://developer.chrome.com/blog/self-xss and https://issues.chromium.org/issues/41491762 for
            // details.
            chromeConfig.Options.AddArgument("unsafely-disable-devtools-self-xss-warnings");

            // Disables the default search engine selector splash screen.
            chromeConfig.Options.AddArgument("disable-search-engine-choice-screen");

            chromeConfig.Options.SetCommonChromiumOptions(configuration);

            // The current versions can be retrieved here:
            // https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions.json. This version number
            // is updated automatically by Renovate.
            // If anything on this line is changed, be sure to adjust the regex in the renovate.json5 config file in the
            // root too.
            chromeConfig.Options.BrowserVersion = "134.0.6998.165";

            configuration.BrowserOptionsConfigurator?.Invoke(chromeConfig.Options);

            chromeConfig.Service = ChromeDriverService.CreateDefaultService();

            chromeConfig.Service.SuppressInitialDiagnosticInformation = true;
            // By default localhost is only allowed in IPv4.
            chromeConfig.Service.AllowedIPAddresses += "::ffff:127.0.0.1";
            // Helps with misconfigured hosts.
            if (chromeConfig.Service.HostName == "localhost") chromeConfig.Service.HostName = "127.0.0.1";

            configuration.Arguments.SetItems(chromeConfig.Options.Arguments);

            return new ChromeDriver(chromeConfig.Service, chromeConfig.Options, pageLoadTimeout)
                .SetCommonTimeouts(pageLoadTimeout);
        }));

    public static Task<Func<EdgeDriver>> CreateEdgeDriverAsync(BrowserConfiguration configuration, TimeSpan pageLoadTimeout) =>
        CreateDriverAsync(() =>
        {
            var options = new EdgeOptions().SetCommonOptions();

            options.SetCommonChromiumOptions(configuration);

            // The current versions can be retrieved here: https://edgeupdates.microsoft.com/api/products. This version
            // number is updated automatically by Renovate.
            // If anything on these lines changed, be sure to adjust the regex in the renovate.json5 config file in the
            // root too.
            if (OperatingSystem.IsLinux())
            {
                var linuxEdgeVersion = "134.0.3124.83";
                options.BrowserVersion = linuxEdgeVersion;
            }
            else if (OperatingSystem.IsWindows())
            {
                var windowsEdgeVersion = "134.0.3124.83";
                options.BrowserVersion = windowsEdgeVersion;
            }
            else if (!OperatingSystem.IsMacOS())
            {
                var macOsEdgeVersion = "134.0.3124.83";
                options.BrowserVersion = macOsEdgeVersion;
            }

            configuration.BrowserOptionsConfigurator?.Invoke(options);

            var service = EdgeDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;

            configuration.Arguments.SetItems(options.Arguments);

            return Task.FromResult(() => new EdgeDriver(service, options).SetCommonTimeouts(pageLoadTimeout));
        });

    public static Task<Func<FirefoxDriver>> CreateFirefoxDriverAsync(BrowserConfiguration configuration, TimeSpan pageLoadTimeout) =>
        CreateDriverAsync(() => Task.FromResult(() =>
        {
            var firefoxOptions = new FirefoxOptions().SetCommonOptions();

            firefoxOptions.SetPreference("intl.accept_languages", configuration.AcceptLanguage.ToString());

            // Disabling smooth scrolling to avoid large waiting time when taking full-page screenshots.
            firefoxOptions.SetPreference("general.smoothScroll", preferenceValue: false);

            // Disabling hardware acceleration to avoid hardware dependent issues in rendering and visual validation.
            firefoxOptions.SetPreference("browser.preferences.defaultPerformanceSettings.enabled", preferenceValue: false);
            firefoxOptions.SetPreference("layers.acceleration.disabled", preferenceValue: true);

            // Set the download path to inside the context-specific temp directory to avoid clashes from parallel
            // tests, and to make it available for test dumps.
            firefoxOptions.SetPreference("browser.download.folderList", 2);
            firefoxOptions.SetPreference("browser.download.dir", PrepareDownloadDirectory(configuration));
            firefoxOptions.SetPreference("browser.download.useDownloadDir", preferenceValue: true);
            firefoxOptions.SetPreference("pdfjs.disabled", preferenceValue: true);

            // The current versions can be retrieved here:
            // https://product-details.mozilla.org/1.0/firefox_versions.json. This version number is updated
            // automatically by Renovate.
            // If anything on this line is changed, be sure to adjust the regex in the renovate.json5 config file in the
            // root too.
            firefoxOptions.BrowserVersion = "136.0.2";

            if (configuration.Headless) firefoxOptions.AddArgument("--headless");

            configuration.BrowserOptionsConfigurator?.Invoke(firefoxOptions);

            // For some reason FirefoxOptions does not expose the argument list like the Chromium-based driver options
            // classes do.
            const string argumentsFieldName = "firefoxArguments";
            var arguments = typeof(FirefoxOptions)
                .GetField(argumentsFieldName, BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(firefoxOptions) as IList<string> ?? [];
            configuration.Arguments.SetItems(arguments);

            return new FirefoxDriver(firefoxOptions).SetCommonTimeouts(pageLoadTimeout);
        }));

    private static TDriverOptions SetCommonOptions<TDriverOptions>(this TDriverOptions driverOptions)
        where TDriverOptions : DriverOptions
    {
        driverOptions.AcceptInsecureCertificates = true;
        driverOptions.UnhandledPromptBehavior = UnhandledPromptBehavior.Ignore;
        driverOptions.PageLoadStrategy = PageLoadStrategy.Normal;
        driverOptions.UseWebSocketUrl = true;
        return driverOptions;
    }

    private static TDriverOptions SetCommonChromiumOptions<TDriverOptions>(
        this TDriverOptions options,
        BrowserConfiguration configuration)
        where TDriverOptions : ChromiumOptions
    {
        options.AddArgument("--lang=" + configuration.AcceptLanguage);

        // Disabling hardware acceleration to avoid hardware dependent issues in rendering and visual validation.
        options.AddArgument("disable-accelerated-2d-canvas");
        options.AddArgument("disable-gpu");

        // Setting font rendering to keep the text as they are for visual verification testing.
        options.AddArgument("font-render-hinting=none");
        options.AddArgument("disable-font-subpixel-positioning");
        options.AddArgument("disable-lcd-text");

        // Setting color profile explicitly to sRGB to keep colors as they are for visual verification testing.
        options.AddArgument("force-color-profile=sRGB");

        // Disabling DPI scaling.
        options.AddArgument("force-device-scale-factor=1");
        options.AddArgument("high-dpi-support=1");

        // Disabling smooth scrolling to avoid large waiting time when taking full-page screenshots.
        options.AddArgument("disable-smooth-scrolling");

        // Previously this switch caused Chrome processes to remain open after test execution, see
        // https://github.com/Lombiq/UI-Testing-Toolbox/issues/356, but it doesn't seem to be case anymore.
        // Additionally, Ubuntu 2024-based GitHub Actions runners seem to require this flag to be set, see
        // https://github.com/actions/runner-images/issues/8268#issuecomment-2343831000.
        options.AddArgument("--no-sandbox");

        // The prompt requesting notifications may obscure UI elements.
        options.AddArgument("--disable-notifications");

        if (configuration.FakeVideoSource is not null)
        {
            var fakeCameraSourceFilePath = configuration.FakeVideoSource.SaveVideoToTempFolder();

            // In some cases the video would not start automatically. To avoid this scenario we are adding the
            // "disable-gesture-requirement-for-media-playback" flag.
            options.AddArgument("disable-gesture-requirement-for-media-playback");
            options.AddArgument("use-fake-device-for-media-stream");
            options.AddArgument("use-fake-ui-for-media-stream");
            options.AddArgument($"use-file-for-fake-video-capture={fakeCameraSourceFilePath}");
        }

        if (configuration.Headless) options.AddArgument("headless");

        // Set the download path to inside the context-specific temp directory to avoid clashes from parallel tests, and
        // to make it available for test dumps.
        options.AddUserProfilePreference("download.default_directory", PrepareDownloadDirectory(configuration));

        return options;
    }

    private static TDriver SetCommonTimeouts<TDriver>(this TDriver driver, TimeSpan pageLoadTimeout)
        where TDriver : IWebDriver
    {
        // Setting timeouts for cases when tests randomly hang up a bit more for some reason (like the test machine load
        // momentarily spiking). We're not increasing ImplicitlyWait, the default of which is 0, since that would make
        // all tests slower.
        // See: https://stackoverflow.com/a/7312740/220230
        var timeouts = driver.Manage().Timeouts();
        // Default is 5 minutes.
        timeouts.PageLoad = pageLoadTimeout;
        return driver;
    }

    private static async Task<Func<TDriver>> CreateDriverAsync<TDriver>(Func<Task<Func<TDriver>>> driverFactory)
        where TDriver : IWebDriver
    {
        try
        {
            return await driverFactory();
        }
        catch (InvalidDataException exception) when (exception.Message.Contains("End of Central Directory record could not be found."))
        {
            throw new WebDriverException(
                $"The web driver extraction failed with the message \"{exception.Message}\". This can indicate the " +
                $"problem with the server that hosts the driver, or with the download URL. Full exception: {exception}",
                exception);
        }
        catch (Exception exception)
        {
            throw new WebDriverException(
                $"Creating the web driver failed with the message \"{exception.Message}\". This can mean that there is a " +
                $"leftover web driver process that you have to kill manually. Full exception: {exception}",
                exception);
        }
    }

    private static string PrepareDownloadDirectory(BrowserConfiguration configuration)
    {
        var downloadPath = DirectoryPaths.GetTempDirectoryPath(configuration.UITestContextId, DirectoryPaths.Downloads);
        FileSystemHelper.EnsureDirectoryExists(downloadPath);
        return downloadPath;
    }

    private sealed class ChromeConfiguration
    {
        public ChromeOptions Options { get; init; }
        public ChromeDriverService Service { get; set; }
    }
}

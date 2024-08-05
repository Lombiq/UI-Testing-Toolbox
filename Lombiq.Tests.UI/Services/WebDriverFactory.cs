using Atata.WebDriverSetup;
using Lombiq.HelpfulLibraries.Cli.Helpers;
using Lombiq.Tests.UI.Extensions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

// The awkward async-method-that-returns-a-func pattern is used here because the WebDriver factory method required by
// Atata is synchronous but we need async I/O for the initialization before that.
public static class WebDriverFactory
{
    private static readonly object _setupLock = new();

    public static Task<Func<ChromeDriver>> CreateChromeDriverAsync(BrowserConfiguration configuration, TimeSpan pageLoadTimeout)
    {
        Task<Func<ChromeDriver>> CreateDriverInnerAsync(string driverPath = null) =>
            Task.FromResult(() =>
            {
                // Note that no-sandbox should NOT be used, because it causes Chrome processes to remain open, see
                // https://github.com/Lombiq/UI-Testing-Toolbox/issues/356.

                var chromeConfig = new ChromeConfiguration { Options = new ChromeOptions().SetCommonOptions() };

                chromeConfig.Options.SetLoggingPreference(LogType.Browser, LogLevel.Info);

                // Linux-specific setting, may be necessary for running in containers, see
                // https://developers.google.com/web/tools/puppeteer/troubleshooting#tips for more information.
                chromeConfig.Options.AddArgument("disable-dev-shm-usage"); // #spell-check-ignore-line

                // Disables the "self-XSS" warning in dev tools (when you have to type "allow pasting"), see
                // https://developer.chrome.com/blog/self-xss and https://issues.chromium.org/issues/41491762 for
                // details.
                chromeConfig.Options.AddArgument("unsafely-disable-devtools-self-xss-warnings"); // #spell-check-ignore-line

                // Disables the default search engine selector splash screen.
                chromeConfig.Options.AddArgument("disable-search-engine-choice-screen");

                chromeConfig.Options.SetCommonChromiumOptions(configuration);

                configuration.BrowserOptionsConfigurator?.Invoke(chromeConfig.Options);

                chromeConfig.Service = driverPath == null
                    ? ChromeDriverService.CreateDefaultService()
                    : ChromeDriverService.CreateDefaultService(driverPath);

                chromeConfig.Service.SuppressInitialDiagnosticInformation = true;
                // By default localhost is only allowed in IPv4.
                chromeConfig.Service.AllowedIPAddresses += "::ffff:127.0.0.1";
                // Helps with misconfigured hosts.
                if (chromeConfig.Service.HostName == "localhost") chromeConfig.Service.HostName = "127.0.0.1";

                return new ChromeDriver(chromeConfig.Service, chromeConfig.Options, pageLoadTimeout)
                    .SetCommonTimeouts(pageLoadTimeout);
            });

        var chromeWebDriverPath = Environment.GetEnvironmentVariable("CHROMEWEBDRIVER"); // #spell-check-ignore-line
        if (chromeWebDriverPath is { } driverPath && Directory.Exists(driverPath))
        {
            return CreateDriverInnerAsync(driverPath);
        }

        return CreateDriverAsync(BrowserNames.Chrome, () => CreateDriverInnerAsync());
    }

    public static Task<Func<EdgeDriver>> CreateEdgeDriverAsync(BrowserConfiguration configuration, TimeSpan pageLoadTimeout) =>
        CreateDriverAsync<EdgeDriver>(BrowserNames.Edge, async () =>
        {
            var options = new EdgeOptions().SetCommonOptions();

            options.SetCommonChromiumOptions(configuration);

            // While the Edge driver easily locates Edge on Windows, it struggles on Linux, where the different release
            // channels have different executable names. This setting looks up the "microsoft-edge-stable" command and
            // sets the full path as the browser's binary location.
            if (!OperatingSystem.IsWindows() &&
                (await CliWrapHelper.WhichAsync("microsoft-edge-stable"))?.FirstOrDefault() is { } binaryLocation)
            {
                options.BinaryLocation = binaryLocation.FullName;
            }

            configuration.BrowserOptionsConfigurator?.Invoke(options);

            var service = EdgeDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;

            return () => new EdgeDriver(service, options).SetCommonTimeouts(pageLoadTimeout);
        });

    public static Task<Func<FirefoxDriver>> CreateFirefoxDriverAsync(BrowserConfiguration configuration, TimeSpan pageLoadTimeout) =>
        CreateDriverAsync(
            BrowserNames.Firefox,
            () => Task.FromResult(() =>
            {
                var options = new FirefoxOptions().SetCommonOptions();

                options.SetPreference("intl.accept_languages", configuration.AcceptLanguage.ToString());

                // Disabling smooth scrolling to avoid large waiting time when taking full-page screenshots.
                options.SetPreference("general.smoothScroll", preferenceValue: false);

                // Disabling hardware acceleration to avoid hardware dependent issues in rendering and visual validation.
                options.SetPreference("browser.preferences.defaultPerformanceSettings.enabled", preferenceValue: false);
                options.SetPreference("layers.acceleration.disabled", preferenceValue: true);

                if (configuration.Headless) options.AddArgument("--headless");

                configuration.BrowserOptionsConfigurator?.Invoke(options);

                return new FirefoxDriver(options).SetCommonTimeouts(pageLoadTimeout);
            }));

    public static Task<Func<InternetExplorerDriver>> CreateInternetExplorerDriverAsync(
        BrowserConfiguration configuration, TimeSpan pageLoadTimeout) =>
        CreateDriverAsync(BrowserNames.InternetExplorer, () => Task.FromResult(() =>
        {
            var options = new InternetExplorerOptions().SetCommonOptions();

            // IE doesn't support this.
            options.AcceptInsecureCertificates = false;
            configuration.BrowserOptionsConfigurator?.Invoke(options);

            return new InternetExplorerDriver(options).SetCommonTimeouts(pageLoadTimeout);
        }));

    private static TDriverOptions SetCommonOptions<TDriverOptions>(this TDriverOptions driverOptions)
        where TDriverOptions : DriverOptions
    {
        driverOptions.AcceptInsecureCertificates = true;
        driverOptions.PageLoadStrategy = PageLoadStrategy.Normal;
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
        options.AddArgument("disable-gpu"); // #spell-check-ignore-line

        // Setting color profile explicitly to sRGB to keep colors as they are for visual verification testing.
        options.AddArgument("force-color-profile=sRGB");

        // Disabling DPI scaling.
        options.AddArgument("force-device-scale-factor=1");
        options.AddArgument("high-dpi-support=1");

        // Disabling smooth scrolling to avoid large waiting time when taking full-page screenshots.
        options.AddArgument("disable-smooth-scrolling");

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

    private static async Task<Func<TDriver>> CreateDriverAsync<TDriver>(string browserName, Func<Task<Func<TDriver>>> driverFactory)
        where TDriver : IWebDriver
    {
        try
        {
            AutoSetup(browserName);
            return await driverFactory();
        }
        catch (Exception ex)
        {
            throw new WebDriverException(
                $"Creating the web driver failed with the message \"{ex.Message}\". This can mean that there is a " +
                $"leftover web driver process that you have to kill manually. Full exception: {ex}",
                ex);
        }
    }

    // We don't use the async version of auto setup because it doesn't do any locking. In fact it's just the sync method
    // passed to Task.Run() so it wouldn't benefit us anyway.
    private static void AutoSetup(string browserName)
    {
        lock (_setupLock) DriverSetup.AutoSetUp(browserName);
    }

    private sealed class ChromeConfiguration
    {
        public ChromeOptions Options { get; init; }
        public ChromeDriverService Service { get; set; }
    }
}

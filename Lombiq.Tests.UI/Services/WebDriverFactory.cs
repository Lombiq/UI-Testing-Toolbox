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
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

public static class WebDriverFactory
{
    private static readonly object _setupLock = new();

    public static Task<ChromeDriver> CreateChromeDriverAsync(BrowserConfiguration configuration, TimeSpan pageLoadTimeout)
    {
        Task<ChromeDriver> CreateDriverInnerAsync(ChromeDriverService service)
        {
            var chromeConfig = new ChromeConfiguration { Options = new ChromeOptions().SetCommonOptions() };

            chromeConfig.Options.SetLoggingPreference(LogType.Browser, LogLevel.Info);

            // Disabling the Chrome sandbox can speed things up a bit, so it's recommended when you get a lot of
            // timeouts during parallel execution:
            // https://stackoverflow.com/questions/22322596/selenium-error-the-http-request-to-the-remote-webdriver-timed-out-after-60-sec
            // However, this makes the executing machine vulnerable to browser-based attacks so it should only be used
            // with trusted code (like our own).
            chromeConfig.Options.AddArgument("no-sandbox");

            // Linux-specific setting, may be necessary for running in containers, see
            // https://developers.google.com/web/tools/puppeteer/troubleshooting#tips for more information.
            chromeConfig.Options.AddArgument("disable-dev-shm-usage"); // #spell-check-ignore-line

            chromeConfig.Options.SetCommonChromiumOptions(configuration);

            configuration.BrowserOptionsConfigurator?.Invoke(chromeConfig.Options);

            chromeConfig.Service = service ?? ChromeDriverService.CreateDefaultService();
            chromeConfig.Service.SuppressInitialDiagnosticInformation = true;
            // By default localhost is only allowed in IPv4.
            chromeConfig.Service.WhitelistedIPAddresses += "::ffff:127.0.0.1";
            // Helps with misconfigured hosts.
            if (chromeConfig.Service.HostName == "localhost") chromeConfig.Service.HostName = "127.0.0.1";

            return Task.FromResult(
                new ChromeDriver(chromeConfig.Service, chromeConfig.Options, pageLoadTimeout)
                    .SetCommonTimeouts(pageLoadTimeout));
        }

        var chromeWebDriverPath = Environment.GetEnvironmentVariable("CHROMEWEBDRIVER"); // #spell-check-ignore-line
        if (chromeWebDriverPath is { } driverPath && Directory.Exists(driverPath))
        {
            return CreateDriverInnerAsync(ChromeDriverService.CreateDefaultService(driverPath));
        }

        return CreateDriverAsync(BrowserNames.Chrome, () => CreateDriverInnerAsync(service: null));
    }

    public static Task<EdgeDriver> CreateEdgeDriverAsync(BrowserConfiguration configuration, TimeSpan pageLoadTimeout) =>
        CreateDriverAsync(BrowserNames.Edge, async () =>
        {
            var options = new EdgeOptions().SetCommonOptions();

            options.SetCommonChromiumOptions(configuration);

            // While the Edge driver easily locates Edge on Windows, it struggles on Linux, where the different release
            // channels have different executable names. This setting looks up the "microsoft-edge-stable" command and
            // sets the full path as the browser's binary location.
            if (!OperatingSystem.IsOSPlatform(nameof(OSPlatform.Windows)) &&
                (await CliWrapHelper.WhichAsync("microsoft-edge-stable"))?.FirstOrDefault() is { } binaryLocation)
            {
                options.BinaryLocation = binaryLocation.FullName;
            }

            configuration.BrowserOptionsConfigurator?.Invoke(options);

            var service = EdgeDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;

            return new EdgeDriver(service, options).SetCommonTimeouts(pageLoadTimeout);
        });

    public static Task<FirefoxDriver> CreateFirefoxDriverAsync(BrowserConfiguration configuration, TimeSpan pageLoadTimeout)
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

        return CreateDriverAsync(
            BrowserNames.Firefox,
            () => Task.FromResult(new FirefoxDriver(options).SetCommonTimeouts(pageLoadTimeout)));
    }

    public static Task<InternetExplorerDriver> CreateInternetExplorerDriverAsync(BrowserConfiguration configuration, TimeSpan pageLoadTimeout) =>
        CreateDriverAsync(BrowserNames.InternetExplorer, () =>
        {
            var options = new InternetExplorerOptions().SetCommonOptions();

            // IE doesn't support this.
            options.AcceptInsecureCertificates = false;
            configuration.BrowserOptionsConfigurator?.Invoke(options);

            return Task.FromResult(new InternetExplorerDriver(options).SetCommonTimeouts(pageLoadTimeout));
        });

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

    private static async Task<TDriver> CreateDriverAsync<TDriver>(string browserName, Func<Task<TDriver>> driverFactory)
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

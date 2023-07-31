using Atata.WebDriverSetup;
using Lombiq.Tests.UI.Extensions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;

namespace Lombiq.Tests.UI.Services;

public static class WebDriverFactory
{
    private static readonly ConcurrentDictionary<string, Lazy<string>> _driverSetups = new();

    public static ChromeDriver CreateChromeDriver(BrowserConfiguration configuration, TimeSpan pageLoadTimeout)
    {
        ChromeDriver CreateDriverInner(ChromeDriverService service)
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

            return new ChromeDriver(chromeConfig.Service, chromeConfig.Options, pageLoadTimeout).SetCommonTimeouts(pageLoadTimeout);
        }

        var chromeWebDriverPath = Environment.GetEnvironmentVariable("CHROMEWEBDRIVER"); // #spell-check-ignore-line
        if (chromeWebDriverPath is { } driverPath && Directory.Exists(driverPath))
        {
            return CreateDriverInner(ChromeDriverService.CreateDefaultService(driverPath));
        }

        double Measure(Action action)
        {
            var start = DateTime.Now;
            for (int i = 0; i < 10; i++) action();
            return (DateTime.Now - start).TotalSeconds;
        }

        var timeWithLazy = Measure(() => CreateDriver(BrowserNames.Chrome, () => CreateDriverInner(service: null), lazy: true).Dispose());
        var timeDirectly = Measure(() => CreateDriver(BrowserNames.Chrome, () => CreateDriverInner(service: null), lazy: false).Dispose());

        // timeWithLazy: 11,1512399; timeDirectly: 11,0381774

        return CreateDriver(BrowserNames.Chrome, () => CreateDriverInner(service: null));
    }

    public static EdgeDriver CreateEdgeDriver(BrowserConfiguration configuration, TimeSpan pageLoadTimeout) =>
        CreateDriver(BrowserNames.Edge, () =>
        {
            var options = new EdgeOptions().SetCommonOptions();

            options.SetCommonChromiumOptions(configuration);

            configuration.BrowserOptionsConfigurator?.Invoke(options);

            var service = EdgeDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;

            return new EdgeDriver(service, options).SetCommonTimeouts(pageLoadTimeout);
        });

    public static FirefoxDriver CreateFirefoxDriver(BrowserConfiguration configuration, TimeSpan pageLoadTimeout)
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

        return CreateDriver(BrowserNames.Firefox, () => new FirefoxDriver(options).SetCommonTimeouts(pageLoadTimeout));
    }

    public static InternetExplorerDriver CreateInternetExplorerDriver(BrowserConfiguration configuration, TimeSpan pageLoadTimeout) =>
        CreateDriver(BrowserNames.InternetExplorer, () =>
        {
            var options = new InternetExplorerOptions().SetCommonOptions();

            // IE doesn't support this.
            options.AcceptInsecureCertificates = false;
            configuration.BrowserOptionsConfigurator?.Invoke(options);

            return new InternetExplorerDriver(options).SetCommonTimeouts(pageLoadTimeout);
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

    private static TDriver CreateDriver<TDriver>(string browserName, Func<TDriver> driverFactory, bool lazy = true)
        where TDriver : IWebDriver
    {
        // We could just use VersionResolveStrategy.MatchingBrowser as this is what DriverManager.SetUpDriver() does.
        // But this way the version is also stored and can be used in the exception message if there is a problem.
        var version = "<UNKNOWN>";

        try
        {
            // While SetUpDriver() does locking and caches the driver it's faster not to do any of that if the setup was
            // already done. For 100 such calls it's around 16s vs <100ms. The Lazy<T> trick taken from:
            // https://stackoverflow.com/a/31637510/220230
            version = lazy
                ? _driverSetups.GetOrAdd(browserName, name => new Lazy<string>(() => DriverSetup.AutoSetUp(name).Version)).Value
                : DriverSetup.AutoSetUp(browserName).Version;

            return driverFactory();
        }
        catch (WebException ex)
        {
            throw new WebDriverException(
                $"Failed to download the web driver version {version} with the message \"{ex.Message}\". If it's a " +
                $"404 error, then likely there is no driver available for your specific browser version.",
                ex);
        }
        catch (Exception ex)
        {
            throw new WebDriverException(
                $"Creating the web driver failed with the message \"{ex.Message}\". This can mean that there is a " +
                $"leftover web driver process that you have to kill manually. Full exception: {ex}",
                ex);
        }
    }

    private sealed class ChromeConfiguration
    {
        public ChromeOptions Options { get; init; }
        public ChromeDriverService Service { get; set; }
    }
}

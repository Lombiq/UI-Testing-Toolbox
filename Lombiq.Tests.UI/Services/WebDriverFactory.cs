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
using System.Net.Http;
using System.Runtime.InteropServices;
using WebDriverManager;
using WebDriverManager.DriverConfigs;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;
using Architecture = WebDriverManager.Helpers.Architecture;

namespace Lombiq.Tests.UI.Services;

public static class WebDriverFactory
{
    private static readonly ConcurrentDictionary<string, Lazy<bool>> _driverSetups = new();

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
            chromeConfig.Options.AddArgument("disable-dev-shm-usage");

            chromeConfig.Options.SetCommonChromiumOptions(configuration);

            configuration.BrowserOptionsConfigurator?.Invoke(chromeConfig.Options);

            chromeConfig.Service = service ?? ChromeDriverService.CreateDefaultService();
            chromeConfig.Service.SuppressInitialDiagnosticInformation = true;
            chromeConfig.Service.WhitelistedIPAddresses += "::ffff:127.0.0.1"; // By default localhost is only allowed in IPv4.
            if (chromeConfig.Service.HostName == "localhost") chromeConfig.Service.HostName = "127.0.0.1"; // Helps with misconfigured hosts.

            return new ChromeDriver(chromeConfig.Service, chromeConfig.Options, pageLoadTimeout).SetCommonTimeouts(pageLoadTimeout);
        }

        if (Environment.GetEnvironmentVariable("CHROMEWEBDRIVER") is { } driverPath && Directory.Exists(driverPath))
        {
            return CreateDriverInner(ChromeDriverService.CreateDefaultService(driverPath));
        }

        return CreateDriver(new ChromeConfig(), () => CreateDriverInner(service: null));
    }

    public static EdgeDriver CreateEdgeDriver(BrowserConfiguration configuration, TimeSpan pageLoadTimeout) =>
        CreateDriver(new CustomEdgeConfig(), () =>
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

        return CreateDriver(new FirefoxConfig(), () => new FirefoxDriver(options).SetCommonTimeouts(pageLoadTimeout));
    }

    public static InternetExplorerDriver CreateInternetExplorerDriver(BrowserConfiguration configuration, TimeSpan pageLoadTimeout) =>
        CreateDriver(new InternetExplorerConfig(), () =>
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
        options.AddArgument("disable-gpu");

        // Setting color profile explicitly to sRGB to keep colors as they are for visual verification testing.
        options.AddArgument("force-color-profile=sRGB");

        // Disabling DPI scaling.
        options.AddArgument("force-device-scale-factor=1");
        options.AddArgument("high-dpi-support=1");

        // Disabling smooth scrolling to avoid large waiting time when taking full-page screenshots.
        options.AddArgument("disable-smooth-scrolling");

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

    private static TDriver CreateDriver<TDriver>(IDriverConfig driverConfig, Func<TDriver> driverFactory)
        where TDriver : IWebDriver
    {
        // We could just use VersionResolveStrategy.MatchingBrowser as this is what DriverManager.SetUpDriver() does.
        // But this way the version is also stored and can be used in the exception message if there is a problem.
        var version = "<UNKNOWN>";

        try
        {
            // Firefox: The FirefoxConfig.GetMatchingBrowserVersion() resolves the browser version but not the
            // geckodriver version.
            version = driverConfig is FirefoxConfig
                ? driverConfig.GetLatestVersion()
                : driverConfig.GetMatchingBrowserVersion();

            // While SetUpDriver() does locking and caches the driver it's faster not to do any of that if the setup was
            // already done. For 100 such calls it's around 16s vs <100ms. The Lazy<T> trick taken from:
            // https://stackoverflow.com/a/31637510/220230
            _ = _driverSetups.GetOrAdd(driverConfig.GetName(), _ => new Lazy<bool>(() =>
            {
                new DriverManager().SetUpDriver(driverConfig, version);
                return true;
            })).Value;

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

    // This is because of the WebDriverManager.DriverConfigs.Impl.EdgeConfig in WebDriverManager doesn't support Edge on
    // Linux. WebDriverManager issue: https://github.com/rosolko/WebDriverManager.Net/issues/196
    private sealed class CustomEdgeConfig : IDriverConfig
    {
        public string GetName() => "Edge";

        public string GetBinaryName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "msedgedriver";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "msedgedriver.exe";
            }

            throw new PlatformNotSupportedException("Your operating system is not supported");
        }

        public string GetUrl32() => GetUrl(Architecture.X32);

        public string GetUrl64() => GetUrl(Architecture.X64);

        public string GetLatestVersion() => GetLatestVersion("https://msedgedriver.azureedge.net/LATEST_STABLE");

        private static string GetLatestVersion(string url)
        {
            var uri = new Uri(url);
            using var client = new HttpClient();

            return client.GetStringAsync(uri).Result.Trim();
        }

        public string GetMatchingBrowserVersion()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return RegistryHelper.GetInstalledBrowserVersionLinux("microsoft-edge", "--version");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RegistryHelper.GetInstalledBrowserVersionWin("msedge.exe");
            }

            throw new PlatformNotSupportedException("Your operating system is not supported");
        }

        private static string GetUrl(Architecture architecture)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && architecture == Architecture.X64)
            {
                return $"https://msedgedriver.azureedge.net/<version>/edgedriver_linux{((int)architecture).ToTechnicalString()}.zip";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $"https://msedgedriver.azureedge.net/<version>/edgedriver_win{((int)architecture).ToTechnicalString()}.zip";
            }

            throw new PlatformNotSupportedException("Your operating system is not supported");
        }
    }
}

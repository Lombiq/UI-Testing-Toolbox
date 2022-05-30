using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using WebDriverManager;
using WebDriverManager.DriverConfigs;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace Lombiq.Tests.UI.Services;

public static class WebDriverFactory
{
    private static readonly ConcurrentDictionary<string, Lazy<bool>> _driverSetups = new();

    public static ChromeDriver CreateChromeDriver(BrowserConfiguration configuration, TimeSpan pageLoadTimeout)
    {
        ChromeDriver CreateDriverInner(ChromeDriverService service)
        {
            var chromeConfig = new ChromeConfiguration { Options = new ChromeOptions().SetCommonOptions() };

            chromeConfig.Options.AddArgument("--lang=" + configuration.AcceptLanguage);

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

            // Disabling hardware acceleration to avoid hardware dependent issues in rendering and visual validation.
            chromeConfig.Options.AddArgument("disable-accelerated-2d-canvas");
            chromeConfig.Options.AddArgument("disable-gpu");

            chromeConfig.Options.AddArgument("force-color-profile=sRGB");

            if (configuration.Headless) chromeConfig.Options.AddArgument("headless");

            configuration.BrowserOptionsConfigurator?.Invoke(chromeConfig.Options);

            chromeConfig.Service = service ?? ChromeDriverService.CreateDefaultService();
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
        CreateDriver(new StaticVersionEdgeConfig(), () =>
        {
            // This workaround is necessary for Edge, see: https://github.com/rosolko/WebDriverManager.Net/issues/71
            var config = new StaticVersionEdgeConfig();
            var architecture = ArchitectureHelper.GetArchitecture();
            // Using a hard-coded version for now to use the latest released one instead of canary that would be
            // returned by EdgeConfig.GetLatestVersion(). See:
            // https://github.com/rosolko/WebDriverManager.Net/issues/74
            var version = config.GetLatestVersion();
            var path = FileHelper.GetBinDestination(config.GetName(), version, architecture, config.GetBinaryName());

            var options = new EdgeOptions().SetCommonOptions();

            if (configuration.AcceptLanguage.Name != BrowserConfiguration.DefaultAcceptLanguage.Name)
            {
                options.AddArgument("--lang=" + configuration.AcceptLanguage);
            }

            if (configuration.Headless) options.AddArgument("headless");

            configuration.BrowserOptionsConfigurator?.Invoke(options);

            return
                new EdgeDriver(
                        EdgeDriverService.CreateDefaultService(Path.GetDirectoryName(path), Path.GetFileName(path)),
                        options)
                    .SetCommonTimeouts(pageLoadTimeout);
        });

    public static FirefoxDriver CreateFirefoxDriver(BrowserConfiguration configuration, TimeSpan pageLoadTimeout)
    {
        var options = new FirefoxOptions().SetCommonOptions();

        options.SetPreference("intl.accept_languages", configuration.AcceptLanguage.ToString());

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
            version = driverConfig.GetMatchingBrowserVersion();

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

    private class StaticVersionEdgeConfig : EdgeConfig
    {
        public override string GetLatestVersion() => "83.0.478.37";
    }

    private sealed class ChromeConfiguration
    {
        public ChromeOptions Options { get; init; }
        public ChromeDriverService Service { get; set; }
    }
}

using Microsoft.Win32;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WebDriverManager;
using WebDriverManager.DriverConfigs;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace Lombiq.Tests.UI.Services;

public static class WebDriverFactory
{
    private static readonly ConcurrentDictionary<string, Lazy<bool>> _driverSetups = new();

    public static Task<ChromeDriver> CreateChromeDriverAsync(BrowserConfiguration configuration, TimeSpan pageLoadTimeout)
    {
        var state = new ChromeConfiguration { Options = new ChromeOptions().SetCommonOptions(), Service = null };

        ChromeDriver CreateDriver()
        {
            state.Options.AddArgument("--lang=" + configuration.AcceptLanguage);

            state.Options.SetLoggingPreference(LogType.Browser, LogLevel.Info);

            // Disabling the Chrome sandbox can speed things up a bit, so recommended when you get a lot of timeouts
            // during parallel execution:
            // https://stackoverflow.com/questions/22322596/selenium-error-the-http-request-to-the-remote-webdriver-timed-out-after-60-sec
            // However, this makes the executing machine vulnerable to browser-based attacks so it should only be used
            // with trusted code (like our own).
            state.Options.AddArgument("no-sandbox");

            // Linux-specific setting, may be necessary for running in containers, see
            // https://developers.google.com/web/tools/puppeteer/troubleshooting#tips
            state.Options.AddArgument("disable-dev-shm-usage");

            if (configuration.Headless) state.Options.AddArgument("headless");

            configuration.BrowserOptionsConfigurator?.Invoke(state.Options);

            state.Service ??= ChromeDriverService.CreateDefaultService();
            state.Service.WhitelistedIPAddresses += "::ffff:127.0.0.1"; // By default localhost is only allowed in IPv4.
            if (state.Service.HostName == "localhost") state.Service.HostName = "127.0.0.1"; // Helps with misconfigured hosts.

            return new ChromeDriver(state.Service, state.Options, pageLoadTimeout).SetCommonTimeouts(pageLoadTimeout);
        }

        if (Environment.GetEnvironmentVariable("CHROMEWEBDRIVER") is { } driverPath &&
            Directory.Exists(driverPath))
        {
            state.Service = ChromeDriverService.CreateDefaultService(driverPath);
            return Task.FromResult(CreateDriver());
        }

        return CreateDriverAsync(CreateDriver, new ChromeConfig());
    }

    public static Task<EdgeDriver> CreateEdgeDriverAsync(BrowserConfiguration configuration, TimeSpan pageLoadTimeout) =>
        CreateDriverAsync(
            () =>
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

                return new EdgeDriver(
                        EdgeDriverService.CreateDefaultService(Path.GetDirectoryName(path), Path.GetFileName(path)),
                        options)
                    .SetCommonTimeouts(pageLoadTimeout);
            },
            new StaticVersionEdgeConfig());

    public static Task<FirefoxDriver> CreateFirefoxDriverAsync(BrowserConfiguration configuration, TimeSpan pageLoadTimeout)
    {
        var options = new FirefoxOptions().SetCommonOptions();

        options.SetPreference("intl.accept_languages", configuration.AcceptLanguage.ToString());

        if (configuration.Headless) options.AddArgument("--headless");
        configuration.BrowserOptionsConfigurator?.Invoke(options);

        return CreateDriverAsync(
            () => new FirefoxDriver(options).SetCommonTimeouts(pageLoadTimeout),
            new FirefoxConfig());
    }

    public static Task<InternetExplorerDriver> CreateInternetExplorerDriverAsync(BrowserConfiguration configuration, TimeSpan pageLoadTimeout) =>
        CreateDriverAsync(
            () =>
            {
                var options = new InternetExplorerOptions().SetCommonOptions();

                // IE doesn't support this.
                options.AcceptInsecureCertificates = false;
                configuration.BrowserOptionsConfigurator?.Invoke(options);

                return new InternetExplorerDriver(options).SetCommonTimeouts(pageLoadTimeout);
            },
            new InternetExplorerConfig());

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

    private static async Task<TDriver> CreateDriverAsync<TDriver>(Func<TDriver> driverFactory, IDriverConfig driverConfig)
        where TDriver : IWebDriver
    {
        var version = "<UNKNOWN>";
        try
        {
            version = await TryFindVersionAsync(driverConfig);

            // While SetUpDriver() does locking and caches the driver it's faster not to do any of that if the setup was
            // already done. For 100 such calls it's around 16 s vs <100 ms. The Lazy<T> trick taken from:
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

    private static async Task<string> TryFindVersionAsync(IDriverConfig driverConfig)
    {
        string version;
        try
        {
            version = (driverConfig, GetPlatform()) switch
            {
                (ChromeConfig, Platform.Linux) => RegistryHelper.GetInstalledBrowserVersionLinux("chromium", "--version"),
                (FirefoxConfig, Platform.Linux) => RegistryHelper.GetInstalledBrowserVersionLinux("firefox", "--version"),
                (FirefoxConfig, Platform.Windows) => RegistryHelper.GetInstalledBrowserVersionWin("firefox.exe"),
                (FirefoxConfig, Platform.MacOs) => RegistryHelper.GetInstalledBrowserVersionOsx("Firefox", "--version"),
                (InternetExplorerConfig, Platform.Windows) =>
                    (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Internet Explorer", "svcVersion", "Latest"),
                _ => driverConfig.GetMatchingBrowserVersion(),
            };

            if (version != null && driverConfig is ChromeConfig && Version.TryParse(version, out var chromeVersion))
            {
                // Chrome doesn't always have the driver for the same revision you have installed, especially if you use
                // a package manager instead of the browser's auto-updater. To get around this, interrogate the API for
                // the latest revision of the same build version, as that should be still compatible.
                var versionApiUri = new Uri(
                    "https://chromedriver.storage.googleapis.com/LATEST_RELEASE_" +
                    FormattableString.Invariant($"{chromeVersion.Major}.{chromeVersion.Minor}.{chromeVersion.Build}"));
                using var client = new HttpClient();
                version = await (await client.GetAsync(versionApiUri)).Content.ReadAsStringAsync();
            }
        }
        catch
        {
            return VersionResolveStrategy.Latest;
        }

        return Version.TryParse(version, out _) ? version : VersionResolveStrategy.Latest;
    }

    private static Platform GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return Platform.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return Platform.Linux;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return Platform.MacOs;
        return Platform.Unknown;
    }

    private sealed class ChromeConfiguration
    {
        public ChromeOptions Options { get; init; }
        public ChromeDriverService Service { get; set; }
    }

    private enum Platform
    {
        Unknown,
        Windows,
        Linux,
        MacOs,
    }
}

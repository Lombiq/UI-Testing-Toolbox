using CliWrap;
using CliWrap.Buffered;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WebDriverManager;
using WebDriverManager.DriverConfigs;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace Lombiq.Tests.UI.Services
{
    public static class WebDriverFactory
    {
        private static readonly ConcurrentDictionary<string, Lazy<bool>> _driverSetups = new();

        public static Task<ChromeDriver> CreateChromeDriverAsync(BrowserConfiguration configuration, TimeSpan pageLoadTimeout) =>
            CreateDriverAsync(
                () =>
                {
                    var options = new ChromeOptions().SetCommonOptions();

                    options.AddArgument("--lang=" + configuration.AcceptLanguage);

                    // Disabling the Chrome sandbox can speed things up a bit, so recommended when you get a lot of
                    // timeouts during parallel execution:
                    // https://stackoverflow.com/questions/22322596/selenium-error-the-http-request-to-the-remote-webdriver-timed-out-after-60-sec
                    // However, this makes the executing machine vulnerable to browser-based attacks so it should only
                    // be used with trusted code (i.e. our own).
                    options.AddArgument("no-sandbox");

                    if (configuration.Headless) options.AddArgument("headless");

                    configuration.BrowserOptionsConfigurator?.Invoke(options);

                    return new ChromeDriver(ChromeDriverService.CreateDefaultService(), options, pageLoadTimeout).SetCommonTimeouts(pageLoadTimeout);
                },
                new ChromeConfig());

        public static Task<EdgeDriver> CreateEdgeDriverAsync(BrowserConfiguration configuration, TimeSpan pageLoadTimeout) =>
            CreateDriverAsync(
                () =>
                {
                    // This workaround is necessary for Edge, see:
                    // https://github.com/rosolko/WebDriverManager.Net/issues/71
                    var config = new StaticVersionEdgeConfig();
                    var architecture = ArchitectureHelper.GetArchitecture();
                    // Using a hard-coded version for now to use the latest released one instead of canary that would be
                    // returned by EdgeConfig.GetLatestVersion(). See:
                    // https://github.com/rosolko/WebDriverManager.Net/issues/74
                    var version = config.GetLatestVersion();
                    var path = FileHelper.GetBinDestination(config.GetName(), version, architecture, config.GetBinaryName());

                    var options = new EdgeOptions().SetCommonOptions();

                    // Will be available with Selenium 4: https://stackoverflow.com/a/60894335/220230.
                    if (configuration.AcceptLanguage.Name != BrowserConfiguration.DefaultAcceptLanguage.Name)
                    {
                        throw new NotSupportedException("Edge doesn't support configuring Accept Language.");
                    }

                    // Edge will soon have headless support too, see:
                    // https://techcommunity.microsoft.com/t5/discussions/chromium-edge-automation-with-selenium-best-practice/m-p/436338
                    // Maybe not like this but in Selenium 4 at least.
                    if (configuration.Headless)
                    {
                        throw new NotSupportedException("Edge doesn't support headless mode.");
                    }

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
            where TDriver : RemoteWebDriver
        {
            // Setting timeouts for cases when tests randomly hang up a bit more for some reason (like the test machine
            // load momentarily spiking). We're not increasing ImplicityWait, the default of which is 0, since that
            // would make all tests slower.
            // See: https://stackoverflow.com/a/7312740/220230
            var timeouts = driver.Manage().Timeouts();
            // Default is 5 minutes.
            timeouts.PageLoad = pageLoadTimeout;
            return driver;
        }

        private static async Task<TDriver> CreateDriverAsync<TDriver>(Func<TDriver> driverFactory, IDriverConfig driverConfig)
            where TDriver : RemoteWebDriver
        {
            try
            {
                var version = await TryFindVersionAsync(driverConfig);
                // While SetUpDriver() does locking and caches the driver it's faster not to do any of that if the setup
                // was already done. For 100 such calls it's around 16 s vs <100 ms. The Lazy<T> trick taken from:
                // https://stackoverflow.com/a/31637510/220230
                _ = _driverSetups.GetOrAdd(driverConfig.GetName(), _ => new Lazy<bool>(() =>
                {
                    new DriverManager().SetUpDriver(driverConfig, version);
                    return true;
                })).Value;

                return driverFactory();
            }
            catch (Exception ex)
            {
                throw new WebDriverException(
                    $"Creating the web driver failed with the message \"{ex.Message}\". " +
                    $"Note that this can mean that there is a leftover web driver process that you have to kill manually. Full exception: {ex}",
                    ex);
            }
        }

        private class StaticVersionEdgeConfig : EdgeConfig
        {
            public override string GetLatestVersion() => "83.0.478.37";
        }

        private static async Task<string> TryFindVersionAsync(IDriverConfig driverConfig)
        {
            const string versionArgument = "--version";

            var (executableName, arguments) = driverConfig switch
            {
                ChromeConfig =>
                    (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "chromium" : "chrome", versionArgument),
                FirefoxConfig => ("firefox", versionArgument),
                EdgeConfig => ("powershell", "(Get-AppxPackage Microsoft.MicrosoftEdge).Version"),
                InternetExplorerConfig =>
                    ("reg", @"query HKEY_LOCAL_MACHINE\Software\Microsoft\Internet Explorer /v svcVersion"),
                _ => throw new ArgumentOutOfRangeException(nameof(driverConfig), driverConfig, null),
            };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) executableName += ".exe";

            // Version selection based on the locally installed version is only available for Chrome, see:
            // https://github.com/rosolko/WebDriverManager.Net/pull/91.
            var fallbackVersion = driverConfig is ChromeConfig
                ? VersionResolveStrategy.MatchingBrowser
                : VersionResolveStrategy.Latest;

            try
            {
                var result = await Cli
                    .Wrap(executableName)
                    .WithArguments(arguments)
                    .ExecuteBufferedAsync();
                return result.StandardOutput.Split().FirstOrDefault(word => Version.TryParse(word, out _)) ?? fallbackVersion;
            }
            catch
            {
                return fallbackVersion;
            }
        }
    }
}

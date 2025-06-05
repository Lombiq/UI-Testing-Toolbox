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

            // Disables the "Chrome for Testing vW.X.Y.Z is only for automated testing." and "Chrome is being controlled
            // by automated test software." infobars.
            chromeConfig.Options.AddArgument("disable-infobars");

            chromeConfig.Options.SetCommonChromiumOptions(configuration);

            // The current versions can be retrieved here:
            // https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions.json. This version number
            // is updated automatically by Renovate.
            // If anything on this line is changed, be sure to adjust the regex in the renovate.json5 config file in the
            // root too.
            chromeConfig.Options.BrowserVersion = "137.0.7151.55";

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
                var linuxEdgeVersion = "137.0.3296.62";
                options.BrowserVersion = linuxEdgeVersion;
            }
            else if (OperatingSystem.IsWindows())
            {
                var windowsEdgeVersion = "137.0.3296.62";
                options.BrowserVersion = windowsEdgeVersion;
            }
            else if (!OperatingSystem.IsMacOS())
            {
                var macOsEdgeVersion = "137.0.3296.62";
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
            firefoxOptions.BrowserVersion = "139.0";

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
        // For a list of all switches see https://peter.sh/experiments/chromium-command-line-switches/. Also see
        // https://github.com/GoogleChrome/chrome-launcher/blob/main/docs/chrome-flags-for-tools.md for recommended
        // switches; the appropriate ones are used here.

        // --disable-notifications is not used because that could break apps that try to send browser notifications.

        options.AddArgument("lang=" + configuration.AcceptLanguage);

        // Linux-specific setting, may be necessary for running in containers, see
        // https://developers.google.com/web/tools/puppeteer/troubleshooting#tips for more information.
        options.AddArgument("disable-dev-shm-usage");

        // Disables the "self-XSS" warning in dev tools (when you have to type "allow pasting"), see
        // https://developer.chrome.com/blog/self-xss and https://issues.chromium.org/issues/41491762 for
        // details.
        options.AddArgument("unsafely-disable-devtools-self-xss-warnings");

        // Disables the default search engine selector splash screen.
        options.AddArgument("disable-search-engine-choice-screen");

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
        // Additionally, Ubuntu 24.04-based GitHub Actions runners seem to require this flag to be set, see
        // https://github.com/actions/runner-images/issues/8268#issuecomment-2343831000.
        options.AddArgument("no-sandbox");

        // Test user credentials can cause Chromium browsers to display a warning about unsafe passwords, which dims the
        // whole viewport. One of these disabled it but then couldn't reproduce the warning even with all of them
        // removed. So, leaving all of them here, since this won't cause any issues. --suppress-message-center-popups
        // also hides all other toast notifications.
        options.AddArgument("password-store=basic");
        options.AddArgument("use-mock-keychain");
        options.AddUserProfilePreference("credentials_enable_service", "false");
        options.AddUserProfilePreference("profile.password_manager_enabled", "false");
        options.AddUserProfilePreference("reduce-security-for-testing", "null");
        options.AddUserProfilePreference("profile.password_manager_leak_detection", "false");
        options.AddArgument("suppress-message-center-popups");

        // Disables the default browser check, which is useless during UI tests.
        options.AddArgument("no-default-browser-check");

        // Skip First Run tasks as well as not showing additional dialogs, prompts or bubbles.
        options.AddArgument("no-first-run");

        // Disables the service process from adding itself as an autorun process.
        options.AddArgument("no-service-autorun");

        // Disables all extensions, and also some built-in extensions that aren't affected by --disable-extensions.
        options.AddArgument("disable-extensions");
        options.AddArgument("disable-component-extensions-with-background-pages");

        // Disables any of the default Chrome apps.
        options.AddArgument("disable-default-apps");

        // Disables the Discover feed on the New Tab Page.
        options.AddArgument("disable-features=InterestFeedContentSuggestion");

        // Disables Chrome translation, both the manual option and the popup prompt when a page with differing language
        // is detected.
        options.AddArgument("disable-features=Translate");

        // Avoids blue bubble "user education" nudges (eg., "… give your browser a new look", Memory Saver)
        options.AddArgument("ash-no-nudges");

        // Disables all in-product help. See
        // https://chromium.googlesource.com/chromium/src/+/master/components/feature_engagement/README.md.
        options.AddArgument("propagate-iph-for-testing");

        // Disables timers being throttled in background pages/tabs.
        options.AddArgument("disable-background-timer-throttling");

        // Normally, Chrome will treat a 'foreground' tab instead as backgrounded if the surrounding window is occluded
        // (aka visually covered) by another window. This flag disables that.
        options.AddArgument("disable-backgrounding-occluded-windows");

        // Related to the previous one. Calculate window occlusion on Windows will be used in the future to throttle and
        // potentially unload foreground tabs in occluded windows. Disabling that.
        options.AddArgument("disable-features=CalculateNativeWinOcclusion");

        // Suppresses hang monitor dialogs.
        options.AddArgument("disable-hang-monitor");

        // Related to the previous one. Disables non-foreground tabs from getting a lower process priority.
        options.AddArgument("disable-renderer-backgrounding");

        // Disables site isolation between subdomains. Not needed during testing but it increases process count.
        options.AddArgument("disable-features=IsolateOrigins");

        // Related to the previous one. Disables site isolation completely:
        // https://www.chromium.org/Home/chromium-security/site-isolation/.
        options.AddArgument("disable-features=site-per-process");

        // Load all iframes immediately to reduce the flakiness of lazy loading.
        options.AddArgument("disable-features=LazyFrameLoading");

        // Disables the "Enhanced ad privacy in Chrome" dialog.
        options.AddArgument("disable-features=PrivacySandboxSettings4");

        // Disables various background network services, including extension updating, safe browsing service, upgrade
        // detector, translate...
        options.AddArgument("disable-background-networking");

        // Disables the crash reporting.
        options.AddArgument("disable-breakpad");

        // Don't update the browser components listed at chrome://components/.
        options.AddArgument("disable-component-update");

        // Related to the previous one. Disables the updater for
        // https://chromium.googlesource.com/chromium/src/+/lkgr/net/docs/certificate-transparency.md.
        options.AddArgument("disable-features=CertificateTransparencyComponentUpdater");

        // Disables Domain Reliability Monitoring, which tracks whether the browser has difficulty contacting
        // Google-owned sites and uploads reports to Google.
        options.AddArgument("disable-domain-reliability");

        // Disables autofill server communication.
        options.AddArgument("disable-features=AutofillServerCommunication");

        // Disables syncing to a Google account.
        options.AddArgument("disable-sync");

        // Disables reporting to Google User Metrics Analysis (see https://stackoverflow.com/a/39045389), but allows for
        // collection.
        options.AddArgument("metrics-recording-only");

        // Disables the Chrome Optimization Guide
        // (https://chromium.googlesource.com/chromium/src/+/HEAD/components/optimization_guide/) and networking with
        // its service API.
        options.AddArgument("disable-features=OptimizationHints");

        // Avoid the startup dialog for 'Do you want the application “Chromium.app” to accept incoming network
        // connections?'. Also disables the Chrome Media Router which creates background networking activity to discover
        // cast targets.
        options.AddArgument("disable-features=MediaRouter");

        // Making rendering (more) deterministic. --deterministic-mode is supposed to switch on all of these and
        // --run-all-compositor-stages-before-draw, but that flag doesn't seem to exist anymore; so switching everything
        // on manually. --run-all-compositor-stages-before-draw mustn't be used, since it causes
        // "OpenQA.Selenium.WebDriverTimeoutException: timeout: Timed out receiving message from renderer: 10.000"
        // exceptions when taking screenshots under Ubuntu.
        options.AddArgument("disable-new-content-rendering-timeout");
        options.AddArgument("enable-begin-frame-control");
        options.AddArgument("disable-threaded-animation");
        options.AddArgument("disable-threaded-scrolling");
        options.AddArgument("disable-checker-imaging");
        options.AddArgument("disable-image-animation-resync");

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

        // Previously this was only "headless", then "headless=new" (see
        // https://www.selenium.dev/blog/2023/headless-is-going-away/) but now just "headless" is enough too:
        // https://developer.chrome.com/blog/removing-headless-old-from-chrome.
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

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Concurrent;
using WebDriverManager;
using WebDriverManager.DriverConfigs;
using WebDriverManager.DriverConfigs.Impl;

namespace Lombiq.Tests.UI.Services
{
    public static class WebDriverFactory
    {
        private readonly static ConcurrentDictionary<string, Lazy<bool>> _driverSetups = new ConcurrentDictionary<string, Lazy<bool>>();


        public static ChromeDriver CreateChromeDriver(TimeSpan pageLoadTimeout) =>
            CreateDriver(() =>
            {
                var options = new ChromeOptions().SetCommonOptions();

                // Disabling the Chrome sandbox can speed things up a bit, so recommended when you get a lot of timeouts
                // during parallel execution: https://stackoverflow.com/questions/22322596/selenium-error-the-http-request-to-the-remote-webdriver-timed-out-after-60-sec
                // However, this makes the executing machine vulnerable to browser-based attacks so it should only be used
                // with trusted code (i.e. our own).
                options.AddArgument("no-sandbox");

                return new ChromeDriver(ChromeDriverService.CreateDefaultService(), options, pageLoadTimeout).SetCommonTimeouts(pageLoadTimeout);
            }, new ChromeConfig());

        public static EdgeDriver CreateEdgeDriver(TimeSpan pageLoadTimeout) =>
            CreateDriver(
                () => new EdgeDriver(new EdgeOptions().SetCommonOptions()).SetCommonTimeouts(pageLoadTimeout),
                new EdgeConfig());

        public static FirefoxDriver CreateFirefoxDriver(TimeSpan pageLoadTimeout) =>
            CreateDriver(
                () => new FirefoxDriver(new FirefoxOptions().SetCommonOptions()).SetCommonTimeouts(pageLoadTimeout),
                new FirefoxConfig());

        public static InternetExplorerDriver CreateInternetExplorerDriver(TimeSpan pageLoadTimeout) =>
            CreateDriver(() =>
            {
                var options = new InternetExplorerOptions().SetCommonOptions();
                // IE doesn't support this.
                options.AcceptInsecureCertificates = false;
                return new InternetExplorerDriver(options).SetCommonTimeouts(pageLoadTimeout);
            }, new InternetExplorerConfig());


        private static TDriverOptions SetCommonOptions<TDriverOptions>(this TDriverOptions driverOptions) where TDriverOptions : DriverOptions
        {
            driverOptions.AcceptInsecureCertificates = true;
            driverOptions.PageLoadStrategy = PageLoadStrategy.Normal;
            return driverOptions;
        }

        private static TDriver SetCommonTimeouts<TDriver>(this TDriver driver, TimeSpan pageLoadTimeout) where TDriver : RemoteWebDriver
        {
            // Setting timeouts for cases when tests randomly hang up a bit more for some reason (like the test
            // machine load momentarily spiking).
            // We're not increasing ImplicityWait, the default of which is 0, since that would make all tests slower.
            // See: https://stackoverflow.com/a/7312740/220230
            var timeouts = driver.Manage().Timeouts();
            // Default is 5 minutes. 
            timeouts.PageLoad = pageLoadTimeout;
            return driver;
        }

        private static TDriver CreateDriver<TDriver>(Func<TDriver> driverFactory, IDriverConfig driverConfig) where TDriver : RemoteWebDriver
        {
            try
            {
                // While SetUpDriver() does locking and caches the driver it's faster not to do any of that if the setup
                // was already done. For 100 such calls it's around 16 s vs <100 ms.
                // The Lazy<T> trick taken from: https://stackoverflow.com/a/31637510/220230
                _ = _driverSetups.GetOrAdd(driverConfig.GetName(), _ => new Lazy<bool>(() =>
                {
                    new DriverManager().SetUpDriver(driverConfig);
                    return true;
                })).Value;

                return driverFactory();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Creating the web driver failed with the message \"{ex.Message}\". Note that this can mean that there is a leftover web driver process that you have to kill manually.",
                    ex);
            }
        }
    }
}

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Remote;
using System;

namespace Lombiq.Tests.UI.Services
{
    public static class WebDriverFactory
    {
        public static ChromeDriver CreateChromeDriver(TimeSpan pageLoadTimeout)
        {
            var options = new ChromeOptions().SetCommonOptions();

            // Disabling the Chrome sandbox can speed things up a bit, so recommended when you get a lot of timeouts
            // during parallel execution: https://stackoverflow.com/questions/22322596/selenium-error-the-http-request-to-the-remote-webdriver-timed-out-after-60-sec
            // However, this makes the executing machine vulnerable to browser-based attacks so it should only be used
            // with trusted code (i.e. our own).
            options.AddArgument("no-sandbox");

            return new ChromeDriver(".", options, pageLoadTimeout).SetCommonTimeouts(pageLoadTimeout);
        }

        public static EdgeDriver CreateEdgeDriver(TimeSpan pageLoadTimeout) =>
            new EdgeDriver(
                EdgeDriverService.CreateDefaultService(".", "msedgedriver.exe"),
                new EdgeOptions().SetCommonOptions())
            .SetCommonTimeouts(pageLoadTimeout);

        public static FirefoxDriver CreateFirefoxDriver(TimeSpan pageLoadTimeout) =>
            new FirefoxDriver(".", new FirefoxOptions().SetCommonOptions()).SetCommonTimeouts(pageLoadTimeout);

        public static InternetExplorerDriver CreateInternetExplorerDriver(TimeSpan pageLoadTimeout)
        {
            var options = new InternetExplorerOptions().SetCommonOptions();
            // IE doesn't support this.
            options.AcceptInsecureCertificates = false;
            return new InternetExplorerDriver(".", options).SetCommonTimeouts(pageLoadTimeout);
        }


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
    }
}

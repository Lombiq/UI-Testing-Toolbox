using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions
{
    [DebuggerDisplay("{ToString()}")]
    public class BrowserLogMessage
    {
        /// <summary>
        /// Gets or sets the source where the message originates from. This can be ultimately anything but you can find
        /// some common ones under <see cref="Sources"/>.
        /// </summary>
        public string Source { get; set; }

        public MessageLevel Level { get; set; }
        public DateTime DateTimeUtc { get; set; }
        public string Message { get; set; }

        public override string ToString() => $"{DateTimeUtc} UTC | {Level} | {Source} | {Message}";

        public bool IsNotFoundMessage(string url) =>
            Message.ContainsOrdinalIgnoreCase(
                @$"{url} - Failed to load resource: the server responded with a status of 404");

        public enum MessageLevel
        {
            Severe,
            Warning,
            Info,
        }

        public static class Sources
        {
            public const string ConsoleApi = "console-api";
            public const string Deprecation = "deprecation";
            public const string Javascript = "javascript";
            public const string Network = "network";
            public const string Recommendation = "recommendation";
        }
    }

    public static class LoggingWebDriverExtensions
    {
        private static readonly Dictionary<string, BrowserLogMessage.MessageLevel> _levelMappings = new()
        {
            ["SEVERE"] = BrowserLogMessage.MessageLevel.Severe,
            ["WARNING"] = BrowserLogMessage.MessageLevel.Warning,
            ["INFO"] = BrowserLogMessage.MessageLevel.Info,
        };

        private static readonly HttpClient _httpClient = new();

        /// <summary>
        /// Retrieves the console logs from the browser. This log will contain all the log messages since the start of
        /// the session, not just the ones for the current page. NOTE that once you call this the log will be emptied
        /// and only subsequent entries will be included in it. Supports Chrome only.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Code taken from https://stackoverflow.com/a/60436652/220230 and modified. The emptying behavior is not
        /// deliberate, this is just how it works apparently.
        /// </para>
        /// <para>
        /// Direct log retrieval with <c>driver.Manage().Logs.GetLog()</c> (see:
        /// https://stackoverflow.com/a/36463573/220230) currently doesn't work and every log operation throws an NRE.
        /// Apparently this will work again in WebDriver v4 (Selenium 4) which is currently in alpha
        /// (https://www.nuget.org/packages/Selenium.WebDriver/).Then we'll also have access to the Chrome DevTools
        /// console: https://codoid.com/selenium-4-chrome-devtools-log-entry-listeners/. All details accessible from
        /// under
        /// https://stackoverflow.com/questions/57209503/system-nullreferenceexception-when-reading-browser-log-with-selenium.
        /// </para>
        /// <para>For details on log types see: https://github.com/SeleniumHQ/selenium/wiki/Logging#log-types.</para>
        /// </remarks>
        public static async Task<IEnumerable<BrowserLogMessage>> GetAndEmptyBrowserLogAsync(this IWebDriver driver)
        {
            if (driver.GetType() != typeof(ChromeDriver)) return Enumerable.Empty<BrowserLogMessage>();

            var endpoint = GetEndpoint(driver);
            var session = GetSession(driver);
            var resource = $"{endpoint}session/{session}/se/log";
            const string jsonBody = @"{""type"":""browser""}";

            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(resource, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            return AsLogEntries(responseBody)
                .Select(entry => new BrowserLogMessage
                {
                    Source = entry["source"],
                    Level = _levelMappings[entry["level"]],
                    DateTimeUtc = DateTimeOffset
                        .FromUnixTimeMilliseconds(long.Parse(entry["timestamp"], CultureInfo.InvariantCulture))
                        .DateTime,
                    Message = entry["message"],
                });
        }

        private static string GetEndpoint(IWebDriver driver)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

            var remoteWebDriver = GetRemoteWebDriver(driver.GetType());

            var executor = remoteWebDriver.GetField("executor", Flags).GetValue(driver) as DriverServiceCommandExecutor;
            var internalExecutor = executor.GetType().GetField("internalExecutor", Flags).GetValue(executor) as HttpCommandExecutor;

            var uri = internalExecutor.GetType().GetField("remoteServerUri", Flags).GetValue(internalExecutor) as Uri;

            return uri.AbsoluteUri;
        }

        private static Type GetRemoteWebDriver(Type type)
        {
            if (!typeof(RemoteWebDriver).IsAssignableFrom(type)) return type;

            while (type != typeof(RemoteWebDriver))
            {
                type = type.BaseType;
            }

            return type;
        }

        private static SessionId GetSession(IWebDriver driver) =>
            driver is IHasSessionId id ? id.SessionId : new SessionId($"gravity-{Guid.NewGuid()}");

        private static IEnumerable<IDictionary<string, string>> AsLogEntries(string responseBody) =>
            JsonConvert.DeserializeObject<IEnumerable<Dictionary<string, string>>>($"{JToken.Parse(responseBody)["value"]}");
    }
}

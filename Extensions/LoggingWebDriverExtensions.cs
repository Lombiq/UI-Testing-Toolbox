using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// The source where the message originates from. This can be ultimately anything but you can find some common
        /// ones under <see cref="Sources"/>.
        /// </summary>
        public string Source { get; set; }
        public MessageLevel Level { get; set; }
        public DateTime DateTimeUtc { get; set; }
        public string Message { get; set; }


        public override string ToString() =>
            $"{DateTimeUtc} UTC | {Level} | {Source} | {Message}";


        public enum MessageLevel
        {
            Severe,
            Warning,
            Info
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
        private static readonly Dictionary<string, BrowserLogMessage.MessageLevel> _levelMappings = new Dictionary<string, BrowserLogMessage.MessageLevel>
        {
            ["SEVERE"] = BrowserLogMessage.MessageLevel.Severe,
            ["WARNING"] = BrowserLogMessage.MessageLevel.Warning,
            ["INFO"] = BrowserLogMessage.MessageLevel.Info
        };

        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Retrieves the console logs from the browser. NOTE that once you call this the log will be emptied and only
        /// subsequent entries will be included in it. Supports Chrome only.
        /// </summary>
        /// <remarks>
        /// Code taken from https://stackoverflow.com/a/60436652/220230 and modified.The emptying behavior is not
        /// deliberate, this is just how it works apparently.
        /// Direct log retrieval (see: https://stackoverflow.com/a/36463573/220230) currently doesn't work and every log
        /// operation throws an NRE. Apparently this will work again in WebDriver v4 which is currently in alpha
        /// (https://www.nuget.org/packages/Selenium.WebDriver/).Then we'll also have access to the Chrome DevTools
        /// console: https://codoid.com/selenium-4-chrome-devtools-log-entry-listeners/
        /// All details accessible from under
        /// https://stackoverflow.com/questions/57209503/system-nullreferenceexception-when-reading-browser-log-with-selenium.
        /// For details on log types see: https://github.com/SeleniumHQ/selenium/wiki/Logging#log-types
        /// </remarks>
        /// <param name="driver"></param>
        /// <returns></returns>
        public async static Task<IEnumerable<BrowserLogMessage>> GetAndEmptyBrowserLog(this IWebDriver driver)
        {
            if (driver.GetType() != typeof(ChromeDriver)) return Enumerable.Empty<BrowserLogMessage>();

            var endpoint = GetEndpoint(driver);
            var session = GetSession(driver);
            var resource = $"{endpoint}session/{session}/se/log";
            const string jsonBody = @"{""type"":""browser""}";

            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(resource, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            return AsLogEntries(responseBody)
                .Select(entry => new BrowserLogMessage
                {
                    Source = entry["source"],
                    Level = _levelMappings[entry["level"]],
                    DateTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(entry["timestamp"])).DateTime,
                    Message = entry["message"]
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

using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;

namespace Lombiq.Tests.UI.Services
{
    /// <summary>
    /// Retrieves configuration options for tests, provided either statically as system-wide environment variables or at
    /// execution time during temporary session-only environment variables.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reason we're using environment variables because this is the only way apart from files to pass configuration
    /// to xUnit tests, see <see href="https://github.com/xunit/xunit/issues/1908"/>.
    /// </para>
    /// </remarks>
    public static class TestConfigurationManager
    {
        private static readonly JObject _fileConfiguration =
            JObject.Parse(File.Exists("TestConfiguration.json") ? File.ReadAllText("TestConfiguration.json") : "{}");


        public static int GetAgentIndex() => int.Parse(GetConfiguration("AgentIndex", true), CultureInfo.InvariantCulture);

        public static int GetAgentIndexOrDefault() => int.Parse(GetConfiguration("AgentIndex", "0"), CultureInfo.InvariantCulture);

        public static int GetIntConfiguration(string key, int defaultValue) => GetIntConfiguration(key) ?? defaultValue;

        public static int? GetIntConfiguration(string key)
        {
            var config = GetConfiguration(key);
            return string.IsNullOrEmpty(config) ? (int?)null : int.Parse(config, CultureInfo.InvariantCulture);
        }

        public static bool GetBoolConfiguration(string key, bool defaultValue) => GetBoolConfiguration(key) ?? defaultValue;

        public static bool? GetBoolConfiguration(string key)
        {
            var config = GetConfiguration(key);
            return string.IsNullOrEmpty(config) ? (bool?)null : bool.Parse(config);
        }

        // Default value should only be used on null value because an empty string is a valid existing configuration.
        public static string GetConfiguration(string key, string defaultValue) => GetConfiguration(key) ?? defaultValue;

        public static string GetConfiguration(string key, bool throwIfNullOrEmpty = false)
        {
            var prefixedKey = "Lombiq.Tests.UI." + key;
            var config = Environment.GetEnvironmentVariable(prefixedKey);

            if (string.IsNullOrEmpty(config))
            {
                config = _fileConfiguration[key]?.ToString();
            }

            return throwIfNullOrEmpty && string.IsNullOrEmpty(config)
                ? throw new InvalidOperationException($"The configuration with the key {key} was null or empty.")
                : config;
        }
    }
}

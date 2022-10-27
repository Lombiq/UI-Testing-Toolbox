using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using System.IO;

namespace Lombiq.Tests.UI.Services;

/// <summary>
/// Retrieves configuration options for tests, provided either statically as system-wide environment variables or at
/// execution time during temporary session-only environment variables.
/// </summary>
/// <remarks>
/// <para>
/// The reason we're using environment variables because this is the only way apart from files to pass configuration to
/// xUnit tests, see <see href="https://github.com/xunit/xunit/issues/1908"/>.
/// </para>
/// </remarks>
public static class TestConfigurationManager
{
    /// <summary>
    /// Gets the root <see cref="IConfiguration"/> that you can use to access configuration values from environment
    /// variables, or a local or global TestConfiguration.json file.
    /// </summary>
    public static IConfiguration RootConfiguration { get; } = BuildConfiguration();

    public static int GetAgentIndex() => int.Parse(GetConfiguration("AgentIndex", throwIfNullOrEmpty: true), CultureInfo.InvariantCulture);

    public static int GetAgentIndexOrDefault() => int.Parse(GetConfiguration("AgentIndex", "0"), CultureInfo.InvariantCulture);

    public static int GetIntConfiguration(string key, int defaultValue) => GetIntConfiguration(key) ?? defaultValue;

    public static int? GetIntConfiguration(string key)
    {
        var config = GetConfiguration(key);
        return string.IsNullOrEmpty(config) ? null : int.Parse(config, CultureInfo.InvariantCulture);
    }

    public static bool GetBoolConfiguration(string key, bool defaultValue) => GetBoolConfiguration(key) ?? defaultValue;

    public static bool? GetBoolConfiguration(string key)
    {
        var config = GetConfiguration(key);
        return string.IsNullOrEmpty(config) ? null : bool.Parse(config);
    }

    // Default value should only be used on null value because an empty string is a valid existing configuration.
    public static string GetConfiguration(string key, string defaultValue) => GetConfiguration(key) ?? defaultValue;

    public static string GetConfiguration(string key, bool throwIfNullOrEmpty = false)
    {
        var prefixedKey = "Lombiq_Tests_UI:" + key;
        var config = RootConfiguration.GetValue<string>(prefixedKey);

        return throwIfNullOrEmpty && string.IsNullOrEmpty(config)
            ? throw new InvalidOperationException($"The configuration with the key {prefixedKey} was null or empty.")
            : config;
    }

    public static T GetConfiguration<T>()
        where T : new()
    {
        var result = new T();

        var prefixedKey = "Lombiq_Tests_UI:" + typeof(T).Name;
        RootConfiguration.Bind(prefixedKey, result);

        return result;
    }

    private static IConfiguration BuildConfiguration()
    {
        var sharedTestConfigurationPath =
            Environment.GetEnvironmentVariable("LOMBIQ_UI_TESTING_TOOLBOX_SHARED_TEST_CONFIGURATION");

        var builder = new ConfigurationBuilder()
            .AddJsonFile("TestConfiguration.json", optional: true, reloadOnChange: false);

        if (File.Exists(sharedTestConfigurationPath))
            builder = builder
                .AddJsonFile(sharedTestConfigurationPath, optional: true, reloadOnChange: false);

        return builder
            .AddEnvironmentVariables()
            .Build();
    }
}

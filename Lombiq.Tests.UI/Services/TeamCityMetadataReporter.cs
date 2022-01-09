using Lombiq.Tests.UI.Models;
using System;
using System.IO;

namespace Lombiq.Tests.UI.Services
{
    /// <summary>
    /// Helper to write TeamCity <see href="https://www.jetbrains.com/help/teamcity/reporting-test-metadata.html">test
    /// metadata</see> messages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// We could use the <see href="https://github.com/JetBrains/TeamCity.ServiceMessages">TeamCity.ServiceMessages
    /// library</see> but that seems like an overkill for just this.
    /// </para>
    /// </remarks>
    public static class TeamCityMetadataReporter
    {
        public static void ReportInt(UITestManifest uITestManifest, string name, int number) =>
            ReportNumber(uITestManifest, name, number.ToTechnicalString());

        public static void ReportNumber(UITestManifest uITestManifest, string name, string number) =>
            Report(uITestManifest, name, "number", number);

        public static void ReportText(UITestManifest uITestManifest, string name, string text) =>
            Report(uITestManifest, name, "test", text);

        public static void ReportExternalLink(UITestManifest uITestManifest, string name, string url) =>
            Report(uITestManifest, name, "link", url);

        public static void ReportArtifactLink(UITestManifest uITestManifest, string name, string artifactPath) =>
            Report(uITestManifest, name, "artifact", PreparePath(artifactPath));

        public static void ReportImage(UITestManifest uITestManifest, string name, string imageArtifactPath) =>
            Report(uITestManifest, name, "image", PreparePath(imageArtifactPath));

        public static void ReportVideo(UITestManifest uITestManifest, string name, string videoArtifactPath) =>
            Report(uITestManifest, name, "video", PreparePath(videoArtifactPath));

#pragma warning disable S103 // Lines should not be too long
        public static void Report(UITestManifest uITestManifest, string name, string type, string value)
        {
#pragma warning disable S1226 // Method parameters, caught exceptions and foreach variables' initial values should not be ignored
            value = uITestManifest.Name;
            name += "-Test";
#pragma warning restore S1226 // Method parameters, caught exceptions and foreach variables' initial values should not be ignored

            Console.WriteLine(
                Environment.NewLine +
                $"##teamcity[testMetadata testName='Lombiq.Tests.UI.Samples.Tests.ErrorHandlingTests.ErrorOnLoadedPageShouldHaltTest(browser: Chrome)'" +
                $" name='{Escape(name)}' type='{type}' value='{Escape(value)}']");

            Console.WriteLine(
                Environment.NewLine +
                $"##teamcity[testMetadata testName='Lombiq.Tests.UI.Samples.Tests.ErrorHandlingTests.ErrorOnLoadedPageShouldHaltTest'" +
                $" name='{Escape(name)}' type='{type}' value='{Escape(value)}']");

            Console.WriteLine(
                Environment.NewLine +
                $"##teamcity[testMetadata testName='ErrorHandlingTests.ErrorOnLoadedPageShouldHaltTest'" +
                $" name='{Escape(name)}' type='{type}' value='{Escape(value)}']");

            Console.WriteLine(
                Environment.NewLine +
                $"##teamcity[testMetadata testName='ErrorOnLoadedPageShouldHaltTest'" +
                $" name='{Escape(name)}' type='{type}' value='{Escape(value)}']");

            Console.WriteLine(
                Environment.NewLine +
                $"##teamcity[testMetadata testName='Lombiq.Tests.UI.Samples: Tests.ErrorHandlingTests.ErrorOnLoadedPageShouldHaltTest'" +
                $" name='{Escape(name)}' type='{type}' value='{Escape(value)}']");

            Console.WriteLine(
                Environment.NewLine +
                $"##teamcity[testMetadata testName='Lombiq.Tests.UI.Samples: Tests.ErrorHandlingTests.ErrorOnLoadedPageShouldHaltTest(browser: Chrome)'" +
                $" name='{Escape(name)}' type='{type}' value='{Escape(value)}']");

            Console.WriteLine(
                Environment.NewLine +
                $"##teamcity[testMetadata" +
                $" name='{Escape(name)}' type='{type}' value='{Escape(value)}']");
        }
#pragma warning restore S103 // Lines should not be too long

        // TeamCity needs forward slashes to replacing backslashes if the platform uses that.
        private static string PreparePath(string artifactPath) => artifactPath.Replace(Path.DirectorySeparatorChar, '/');

        // Escaping values for TeamCity, see:
        // https://www.jetbrains.com/help/teamcity/service-messages.html#Escaped+values.
        private static string Escape(string value) => value
            .Replace("|", "||", StringComparison.Ordinal)
            .Replace("'", "|'", StringComparison.Ordinal)
            .Replace("\n", "n", StringComparison.Ordinal)
            .Replace("\r", "|r", StringComparison.Ordinal)
            .Replace(@"\uNNNN", "|0xNNNN", StringComparison.Ordinal)
            .Replace("[", "|[", StringComparison.Ordinal)
            .Replace("]", "|]", StringComparison.Ordinal);
    }
}

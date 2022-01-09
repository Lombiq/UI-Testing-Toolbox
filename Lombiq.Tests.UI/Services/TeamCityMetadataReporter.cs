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
            Report(uITestManifest, name, "text", text);

        public static void ReportExternalLink(UITestManifest uITestManifest, string name, string url) =>
            Report(uITestManifest, name, "link", url);

        public static void ReportArtifactLink(UITestManifest uITestManifest, string name, string artifactPath) =>
            Report(uITestManifest, name, "artifact", PreparePath(artifactPath));

        public static void ReportImage(UITestManifest uITestManifest, string name, string imageArtifactPath) =>
            Report(uITestManifest, name, "image", PreparePath(imageArtifactPath));

        public static void ReportVideo(UITestManifest uITestManifest, string name, string videoArtifactPath) =>
            Report(uITestManifest, name, "video", PreparePath(videoArtifactPath));

        public static void Report(UITestManifest uITestManifest, string name, string type, string value)
        {
            // The only form test metadata is understood by TeamCity after an update is:
            // <test suite name>: <namespace name>.<class name>.<test method name>,
            // e.g.: "Lombiq.Tests.UI.Samples: Lombiq.Tests.UI.Samples.Tests.ErrorHandlingTests.ErrorOnLoadedPageShouldHaltTest".
            // Test parameters can't be added.
            // For the docs see: https://www.jetbrains.com/help/teamcity/service-messages.html#Interpreting+test+names.

            var suiteName = uITestManifest.XunitTest.TestCase.TestMethod.TestClass.TestCollection.TestAssembly.Assembly.Name;
            suiteName = suiteName.Substring(0, suiteName.IndexOf(','));
            var methodFullName = uITestManifest.Name.Substring(0, uITestManifest.Name.IndexOf('('));
            var testName = Escape($"{suiteName}: {methodFullName}");

            // Starting with a line break is sometimes necessary not to mix up these messages in the build output.
            Console.WriteLine(
                Environment.NewLine +
                $"##teamcity[testMetadata testName='{testName}' name='{Escape(name)}' type='{type}' value='{Escape(value)}']");
        }

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

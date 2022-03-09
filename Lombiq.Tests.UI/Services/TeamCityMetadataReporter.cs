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
        public static void ReportInt(string testName, string name, int number) =>
            ReportNumber(testName, name, number.ToTechnicalString());

        public static void ReportNumber(string testName, string name, string number) =>
            Report(testName, name, "number", number);

        public static void ReportText(string testName, string name, string text) =>
            Report(testName, name, "text", text);

        public static void ReportExternalLink(string testName, string name, string url) =>
            Report(testName, name, "link", url);

        public static void ReportArtifactLink(string testName, string name, string artifactPath) =>
            Report(testName, name, "artifact", PreparePath(artifactPath));

        public static void ReportImage(string testName, string name, string imageArtifactPath) =>
            Report(testName, name, "image", PreparePath(imageArtifactPath));

        public static void ReportVideo(string testName, string name, string videoArtifactPath) =>
            Report(testName, name, "video", PreparePath(videoArtifactPath));

        public static void Report(string testName, string name, string type, string value) =>
            // Starting with a line break is sometimes necessary not to mix up these messages in the build output.
            Console.WriteLine(
                Environment.NewLine +
                $"##teamcity[testMetadata testName='{Escape(testName)}' name='{Escape(name)}' type='{type}' value='{Escape(value)}']");

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

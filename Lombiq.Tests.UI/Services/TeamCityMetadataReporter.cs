using System;
using System.Globalization;
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
        public static void ReportInt(string name, int number) => ReportNumber(name, number.ToString(CultureInfo.InvariantCulture));

        public static void ReportNumber(string name, string number) => Report(name, "number", number);

        public static void ReportText(string name, string text) => Report(name, "test", text);

        public static void ReportExternalLink(string name, string url) => Report(name, "link", url);

        public static void ReportArtifactLink(string name, string artifactPath) => Report(name, "artifact", PreparePath(artifactPath));

        public static void ReportImage(string name, string imageArtifactPath) => Report(name, "image", PreparePath(imageArtifactPath));

        public static void ReportVideo(string name, string videoArtifactPath) => Report(name, "video", PreparePath(videoArtifactPath));

        public static void Report(string name, string type, string value) =>
            Console.WriteLine($"##teamcity[testMetadata name='{name}' type='{type}' value='{value}']");

        // TeamCity needs forward slashes to replacing backslashes if the platform uses that.
        private static string PreparePath(string artifactPath) => artifactPath.Replace(Path.DirectorySeparatorChar, '/');
    }
}

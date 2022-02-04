using Lombiq.Tests.UI.Models;
using System;
using System.IO;
using System.Text;

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
            var testName = Escape(uITestManifest.Name);

            // Starting with a line break is sometimes necessary not to mix up these messages in the build output.
            Console.WriteLine(
                Environment.NewLine +
                $"##teamcity[testMetadata testName='Lombiq.Tests.UI.Samples: {testName}' " +
                $"name='{Escape(name)}' type='{type}' value='{Escape(value)}']");
        }

        // TeamCity needs forward slashes to replacing backslashes if the platform uses that.
        private static string PreparePath(string artifactPath) => artifactPath.Replace(Path.DirectorySeparatorChar, '/');

        // Escaping values for TeamCity, see:
        // https://www.jetbrains.com/help/teamcity/service-messages.html#Escaped+values.
        // Taken from the sample code under https://youtrack.jetbrains.com/issue/TW-74546.
        private static string Escape(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var stringBuilder = new StringBuilder(value.Length * 2);

            foreach (var character in value)
                switch (character)
                {
                    case '|':
                        stringBuilder.Append("||");
                        break;
                    case '\'':
                        stringBuilder.Append("|'");
                        break;
                    case '\n':
                        stringBuilder.Append("|n");
                        break;
                    case '\r':
                        stringBuilder.Append("|r");
                        break;
                    case '[':
                        stringBuilder.Append("|[");
                        break;
                    case ']':
                        stringBuilder.Append("|]");
                        break;
                    case '\u0085':
                        stringBuilder.Append("|x");
                        break;
                    case '\u2028':
                        stringBuilder.Append("|l");
                        break;
                    case '\u2029':
                        stringBuilder.Append("|p");
                        break;
                    default:
                        if (character > 127)
                        {
                            stringBuilder.Append($"|0x{(ulong)character:x4}");
                        }
                        else
                        {
                            stringBuilder.Append(character);
                        }

                        break;
                }

            return stringBuilder.ToString();
        }
    }
}

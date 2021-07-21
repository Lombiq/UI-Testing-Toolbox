namespace Lombiq.Tests.UI.Services
{
    public class UITestExecutorFailureDumpConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether the subfolder of each test's dumps will use a shortened name, only
        /// containing the name of the test method, without the name of the test class and its namespace. This is to
        /// overcome the 260 character path length limitations on Windows. Defaults to <see langword="true"/>.
        /// </summary>
        public bool UseShortNames { get; set; } = true;

        public string DumpsDirectoryPath { get; set; } = "FailureDumps";
        public bool CaptureAppSnapshot { get; set; } = true;
        public bool CaptureScreenshot { get; set; } = true;
        public bool CaptureHtmlSource { get; set; } = true;
        public bool CaptureBrowserLog { get; set; } = true;
    }
}

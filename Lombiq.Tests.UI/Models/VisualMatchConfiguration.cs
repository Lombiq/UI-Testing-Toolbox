namespace Lombiq.Tests.UI.Models;

public class VisualMatchConfiguration<TSelf>
    where TSelf : VisualMatchConfiguration<TSelf>
{
    /// <summary>
    /// Sets <see cref="DumpFolderName"/>.
    /// </summary>
    /// <param name="value">Folder name where the files get saved, under the failure dumps folder.</param>
    public TSelf WithDumpFolderName(string value)
    {
        DumpFolderName = value;

        return (TSelf)this;
    }

    /// <summary>
    /// Sets <see cref="FullScreenImageFileName"/>.
    /// </summary>
    /// <param name="value">The full-screen image file name for failure dump.</param>
    public TSelf WithFullScreenImageFileName(string value)
    {
        FullScreenImageFileName = value;

        return (TSelf)this;
    }

    /// <summary>
    /// Sets <see cref="ElementImageFileName"/>.
    /// </summary>
    /// <param name="value">The element screenshot image file name for failure dump.</param>
    public TSelf WithElementImageFileName(string value)
    {
        ElementImageFileName = value;

        return (TSelf)this;
    }

    /// <summary>
    /// Sets <see cref="ReferenceImageFileName"/>.
    /// </summary>
    /// <param name="value">The reference image file name for failure dump.</param>
    public TSelf WithReferenceImageFileName(string value)
    {
        ReferenceImageFileName = value;

        return (TSelf)this;
    }

    /// <summary>
    /// Sets <see cref="CroppedElementImageFileName"/>.
    /// </summary>
    /// <param name="value">The cropped element image file name for failure dump.</param>
    public TSelf WithCroppedElementImageFileName(string value)
    {
        CroppedElementImageFileName = value;

        return (TSelf)this;
    }

    /// <summary>
    /// Sets <see cref="CroppedReferenceImageFileName"/>.
    /// </summary>
    /// <param name="value">The cropped reference image file name for failure dump.</param>
    public TSelf WithCroppedReferenceImageFileName(string value)
    {
        CroppedReferenceImageFileName = value;

        return (TSelf)this;
    }

    /// <summary>
    /// Sets <see cref="DiffImageFileName"/>.
    /// </summary>
    /// <param name="value">The diff image file name for failure dump.</param>
    public TSelf WithDiffImageFileName(string value)
    {
        DiffImageFileName = value;

        return (TSelf)this;
    }

    /// <summary>
    /// Sets <see cref="DiffLogFileName"/>.
    /// </summary>
    /// <param name="value">The diff log file name for failure dump.</param>
    public TSelf WithDiffLogFileName(string value)
    {
        DiffLogFileName = value;

        return (TSelf)this;
    }

    /// <summary>
    /// Sets <see cref="DumpFileNamePrefix"/>.
    /// </summary>
    /// <param name="value">The prefix applied for all file names in the failure dump.</param>
    public TSelf WithDumpFileNamePrefix(string value)
    {
        DumpFileNamePrefix = value;

        return (TSelf)this;
    }

    /// <summary>
    /// Gets folder name where the files get saved, under the failure dumps folder.
    /// </summary>
    public string DumpFolderName { get; private set; } = "VisualVerification";

    /// <summary>
    /// Gets the full-screen image file name for failure dump.
    /// </summary>
    public string FullScreenImageFileName { get; private set; } = "FullScreen.bmp";

    /// <summary>
    /// Gets the element screenshot image file name for failure dump.
    /// </summary>
    public string ElementImageFileName { get; private set; } = "Element.bmp";

    /// <summary>
    /// Gets the reference image file name for failure dump.
    /// </summary>
    public string ReferenceImageFileName { get; private set; } = "Reference.bmp";

    /// <summary>
    /// Gets the cropped element image file name for failure dump.
    /// </summary>
    public string CroppedElementImageFileName { get; private set; } = "Element-cropped.bmp";

    /// <summary>
    /// Gets the cropped reference image file name for failure dump.
    /// </summary>
    public string CroppedReferenceImageFileName { get; private set; } = "Reference-cropped.bmp";

    /// <summary>
    /// Gets the diff image file name for failure dump.
    /// </summary>
    public string DiffImageFileName { get; private set; } = "Diff.bmp";

    /// <summary>
    /// Gets the diff log file name for failure dump.
    /// </summary>
    public string DiffLogFileName { get; private set; } = "Diff.log";

    /// <summary>
    /// Gets the prefix applied for all file names in the failure dump.
    /// </summary>
    public string DumpFileNamePrefix { get; private set; }
}

public class VisualMatchConfiguration : VisualMatchConfiguration<VisualMatchConfiguration> { }

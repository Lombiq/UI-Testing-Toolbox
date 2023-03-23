using System;
using System.IO;

namespace Lombiq.Tests.UI.Models;

public class FakeBrowserVideoSource
{
    /// <summary>
    /// Gets or sets a callback that will be used to obtain the video content <see cref="Stream"/> and will be saved and used
    /// as a fake video capture file for the Chrome browser. The consumer will dispose of  the returned <see cref="Stream"/>
    /// after the callback gets called.
    /// </summary>
    public Func<Stream> StreamProvider { get; set; }

    /// <summary>
    /// Gets or sets the video format of the provided stream.
    /// </summary>
    public FakeBrowserVideoSourceFileFormat Format { get; set; } = FakeBrowserVideoSourceFileFormat.MJpeg;
}

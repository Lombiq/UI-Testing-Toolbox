# Fake video capture source

## Preparing video file

You can use video files as a fake video capture source in Chrome browser of format `y4m` or `mjpeg`.

If you have a video file in eg.: `mp4` format use your preferred video tool to convert it to one of the formats mentioned above. If you don't have a preferred tool, simply use `ffmpeg`.

_Suggestion: use `mjpeg`, it will result in a smaller file size._

```bash
# Convert mp4 to y4m.
ffmpeg -y -i test.mp4 -pix_fmt yuv420p test.y4m

#Convert with resize to 480p.
ffmpeg -y -i test.mp4 -filter:v scale=480:-1 -pix_fmt yuv420p test.y4m

# Convert mp4 to mjpeg.
ffmpeg -y -i test.mp4 test.mjpeg

#Convert with resize to 480p.
ffmpeg -y -i test.mp4 -filter:v scale=480:-1 test.mjpeg

```

### Configuring test

Add the prepared video file to your `csproj` as an `EmbeddedResource`, then configure the test method like below.

```csharp
using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

public class BasicTests : UITestBase
{
    public BasicTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Theory, Chrome]
    public Task TestMethod(Browser browser) =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                // ...
            },
            browser,
            configuration =>
                // Here we set the fake video sorce configuration.
                configuration.BrowserConfiguration.FakeVideoSource = new FakeBrowserVideoSource
                {
                    StreamProvider = () =>
                        // Load video file content from resources.
                        typeof(BasicTests).Assembly.GetManifestResourceStream(typeof(BasicTests), "BasicTest.mjpeg"),
                    // Set the video format.
                    Format = FakeBrowserVideoSourceFileFormat.MJpeg,
                });
}
```

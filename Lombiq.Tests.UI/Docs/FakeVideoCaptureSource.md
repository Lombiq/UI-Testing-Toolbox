# Fake video capture source

Imagine you have an application that uses video sources to access visual information from the user or the environment using [Media Capture and Streams API](https://developer.mozilla.org/en-US/docs/Web/API/Media_Capture_and_Streams_API). The goal can be QR or bar code scanning, user identification, or other computer vision applications. To make sure that future changes to the code do not break anything, we need a way to automate testing. Here, the fake video capture source comes into play.

## Preparing video file

You can use `y4m` or `mjpeg` video files as a fake video capture source in the Chrome browser.

If you have a video file in e.g. `mp4` format, use your preferred video tool to convert it to one of the formats mentioned above. If you don't have a preferred tool, simply use `ffmpeg`.

_Hint: The `mjpeg` format will usually result in a smaller file size._

```bash
# Convert mp4 to y4m.
ffmpeg -y -i test.mp4 -pix_fmt yuv420p test.y4m

# Convert with resize to 480p.
ffmpeg -y -i test.mp4 -vf "scale=480:720" -pix_fmt yuv420p test.y4m

# Convert mp4 to mjpeg.
ffmpeg -y -i test.mp4 test.mjpeg

# Convert with resize to 480p.
ffmpeg -y -i test.mp4 -vf "scale=480:720" test.mjpeg
```

_Warning: Using the `-filter:v scale=480:-1` command might "ruin" the video, resulting in a black screen in the browser without warnings._
 
## Sample

You can find a usage example under [Lombiq Vue.js module for Orchard Core - UI Test Extensions](https://github.com/Lombiq/Orchard-Vue.js/tree/dev/Lombiq.VueJs.Tests.UI).

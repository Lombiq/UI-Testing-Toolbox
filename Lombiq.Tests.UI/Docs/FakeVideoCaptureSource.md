# Fake video capture source

Imagine you have an application that uses video sources to access visual information from the user or the environment using [Media Capture and Streams API](https://developer.mozilla.org/en-US/docs/Web/API/Media_Capture_and_Streams_API). The goal can be QR or bar code scanning, user identification, or other computer vision applications.
To make sure our changes do not break anything else we need a way to automatically test. Here comes the fake video capture source to the picture.

## Preparing video file

You can use video files as a fake video capture source in Chrome browser of format `y4m` or `mjpeg`.

If you have a video file in e.g., `mp4` format, use your preferred video tool to convert it to one of the formats mentioned above. If you don't have a preferred tool, simply use `ffmpeg`.

_Suggestion: use `mjpeg`, it will result in a smaller file size._

```bash
# Convert mp4 to y4m.
ffmpeg -y -i test.mp4 -pix_fmt yuv420p test.y4m

# Convert with resize to 480p.
ffmpeg -y -i test.mp4 -filter:v scale=480:-1 -pix_fmt yuv420p test.y4m

# Convert mp4 to mjpeg.
ffmpeg -y -i test.mp4 test.mjpeg

# Convert with resize to 480p.
ffmpeg -y -i test.mp4 -filter:v scale=480:-1 test.mjpeg

```

## Sample

You can find usage example under [Lombiq Vue.js module for Orchard Core - UI Test Extensions](https://github.com/Lombiq/Orchard-Vue.js/tree/dev/Lombiq.VueJs.Tests.UI).

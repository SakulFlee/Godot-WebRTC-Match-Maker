# Demo: Video Call

[![Go to demo folder](https://img.shields.io/badge/Go_to_demo_folder-2ea44f?style=for-the-badge)](https://github.com/SakulFlee/Godot-WebRTC-Match-Maker/tree/main/Godot%20Project/Demos/VideoCall)

> [!WARNING]  
> For this demo to work you will have to install the `FlashCap` package like so:  
> `dotnet add package FlashCap`

The video calling demo is a more complex demo that shows off real-time communication capabilities with the provided plugins.  
It is by no means perfect, but a MWP (= minimal working product).

## Flow

### Audio sending

1. The AudioStreamRecorder is automatically started upon opening the demo
2. (MatchMaker & WebRTC are initialized)
3. Once the audio channel is open: A Audio tick timer is started
4. Each tick, the audio stream will be split, compressed and send on the audio channel

### Audio receiving

0. Once the audio channel is open, audio packets can be received
1. On an audio packet being received: The data is GZIP decompressed back into a raw audio buffer
2. Then, that audio buffer is made into an `AudioStream` object
3. Lastly, The `AudioStream` is assigned to the `AudioStreamPlayer` and playback is started

### Video sending

1. `FlashCap` lists all video capture devices which are added to the ItemList inside our demo
2. Once a device is selected: That capture device is initialized and readied for captured
3. An event listener is assigned to the capture device to listen for new frames
4. Each new frame is reduced in size, then converted into a JPG, compressed with GZIP and finally send over the network

### Video receiving

0. Once the video channel is open, video packets can be received
1. On a video packet being received: The data is GZIP decompressed, then made into an `Image` (JPG) and finally a `ImageTexture`
2. The `ImageTexture` is assigned to a VideoPlayer's texture property and thus displayed on the next drawing cycle
3. Additionally, the frame index is being displayed

## Known issues

### Audio jitter

Audio _may_ be jittery due to limitations with Godot streams.  
This could be improved with using something like timestamps and syncing to the process cycle better.

### Certain webcams missing

Not all webcams seem to be recognized.  
For example: On Windows, only my actual Laptop webcam shows up and a (broken!) Windows virtual webcam. OBS's virtual camera doesn't show up, nor do other USB camera devices.  
However, on Linux the opposite is true: My actual Laptop camera doesn't show up, but any USB and OBS cameras do.
[FlashCap](https://www.nuget.org/packages/FlashCap) (the C# library we are using to get webcam access) may not be the best choice here.  
However, given our constraints of:

- The given library must be Cross-Platform (at least all desktop platforms, mobile support would be nice to have)
- The given library must work on all platforms equally with the same code
- The given library must give access to the frame data of the camera and not require to record to some file
- The given library must not require some dependency to be pre-installed (like FFMPEG)

This seems to be the only library we could find thus far.  
Other libraries exist and claim to be cross-platform, but most of them rely on `DirectShow` which is a **windows-only** library.  
Others, only let you write to files but not access data directly.

Lastly, FFMPEG does support capturing from webcams on all platforms but would need to be bundled.
[MediaFileProcessor](https://github.com/askatmaster/MediaFileProcessor) does, in theory, support automatically downloading tools like FFMPEG, ImageMagick, etc. however as of writing this , the auto-download does not seem to work reliably yet and breaks.  
Furthermore, from their documentation I couldn't find a way to capture webcams from FFMPEG.

The ideal solution would be for Godot to fully implement [Camera Server](https://docs.godotengine.org/en/stable/classes/class_cameraserver.html).  
However, this currently only works on OSX and iOS.

### Receiving video quality

Video quality from the remote is horribly low.  
This is not an issue with the capturing and processing, but a limitation with WebRTC:  
Sending a whole frame will exceed the WebRTC buffer and will cancel the package from being send.
This causes some weird behaviour where sometimes frames make it through (somehow ...) and most of the time nothing gets through.
Resulting in a very choppy, if at all visible, video.

We are converting the raw bitmap image frame into a JPG for compression and GZIP encode it further to save as much space as possible when preparing a package.
However, even then I found that most of the time any quality bigger than ~360p will always error out.  
The best working, and currently also default set as default, resolution is 240x140px. This seems to be the sweet spot where the image is still somewhat recognizable as a face while keeping the package size low enough.

This could be improved of course, but would go much beyond the scope of this demo (being a **minimal** example) and this project.
Here are some ideas for improving it:

- Split the image into chunks. Encode and send chunks independently to reduce packet size. Reassemble on the receiving end.
- Compare the last frame with current frame and only send the differences to reduce data. Use motion vectors possibly.
- Use a format or compression with even greater size reduction. This may require additional libraries.
- Try to pre-process the image and e.g. reduce the color space or similar. Will require some additional libraries like ImageMagick.

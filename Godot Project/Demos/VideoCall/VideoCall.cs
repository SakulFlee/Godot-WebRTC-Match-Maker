using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using FlashCap;
using Godot;

public partial class VideoCall : Node
{
	private MatchMaker matchMaker;

	private ItemList captureDeviceList;

	private TextureRect localVideo;
	private Label localFrameLabel;
	private string localFrameLabelTemplate;

	private TextureRect remoteVideo;
	private Label remoteFrameLabel;
	private string remoteFrameLabelTemplate;

	private CaptureDevices captureDevices;
	private CaptureDevice captureDevice;
	private VideoCharacteristics characteristics;

	public override void _EnterTree()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");

		GetNode<DebugPanel>("%DebugPanel").matchMaker = matchMaker;
		GetNode<ConnectionPanel>("%ConnectionPanel").matchMaker = matchMaker;

		localVideo = GetNode<TextureRect>("%LocalVideo");
		localFrameLabel = GetNode<Label>("%LabelLocalFrame");
		localFrameLabelTemplate = localFrameLabel.Text;
		localFrameLabel.Text = "Awaiting frames ...";

		captureDeviceList = GetNode<ItemList>("%CaptureDeviceList");
		captureDeviceList.Clear();

		remoteVideo = GetNode<TextureRect>("%RemoteVideo");
		remoteFrameLabel = GetNode<Label>("%LabelRemoteFrame");
		remoteFrameLabelTemplate = remoteFrameLabel.Text;
		remoteFrameLabel.Text = "Awaiting frames ...";
	}

	public override void _Ready()
	{
		matchMaker.OnMessageRaw += OnChannelMessageReceivedRaw;

		captureDevices = new CaptureDevices();
		foreach (var descriptor in captureDevices.EnumerateDescriptors())
		{
			if (descriptor.Characteristics.Length > 0)
			{
				GD.Print($"[VideoCall] Found capture device: {descriptor}");
				captureDeviceList.AddItem(descriptor.ToString());

				GD.Print("[VideoCall] Characteristics:");
				foreach (var characteristics in descriptor.Characteristics)
				{
					GD.Print($"[VideoCall] Found capture device: {characteristics}");
				}
			}
			else
			{
				GD.Print($"[VideoCall] Found capture device with no characteristics! ({descriptor})");
			}
		}
	}

	public override void _Process(double delta)
	{
		if (matchMaker.IsReady() && !matchMaker.RequestSend)
		{
			matchMaker.SendMatchMakerRequest(new MatchMakerRequest()
			{
				name = "VideoCall",
			});

			return;
		}
	}

	private async void OnCaptureDeviceSelected(int index)
	{
		if (captureDevice != null)
		{
			await captureDevice.StopAsync();
		}

		var selectedCaptureDevice = captureDevices.EnumerateDescriptors().ElementAt(index);
		GD.Print($"[VideoCall] Device selected: {selectedCaptureDevice}");

		characteristics = selectedCaptureDevice.Characteristics[0];
		GD.Print($"[VideoCall] Characteristics chosen: {characteristics}");

		captureDevice = await selectedCaptureDevice.OpenAsync(
			characteristics,
			OnNewFrame
		);
		await captureDevice.StartAsync();
		GD.Print("[VideoCall] Capture device opened and started!");
	}

	private void OnNewFrame(PixelBufferScope pixelBuffer)
	{
		// Get the bitmap data from the pixel buffer.
		// This data can be in various formats (such as YUV or RBG) depending on the capture device.
		// We hope that Godot does supports converting whatever format your webcam is outputting natively.
		// However, in some cases, you may need to convert the given format to something Godot understands before creating an Image from it.
		byte[] bitmapData = pixelBuffer.Buffer.ExtractImage();

		// Assign the texture.
		// ⚠️ The texture must be set inside a Godot thread! ⚠️
		var image = imageFromBitMap(bitmapData);

		var imageTexture = ImageTexture.CreateFromImage(image);
		setLocalFrame(imageTexture, pixelBuffer.Buffer.FrameIndex);

		// Send frame
		sendFrame(image, pixelBuffer.Buffer.FrameIndex);
	}

	private Image imageFromBitMap(byte[] bitmapData)
	{
		// Create an image from the bitmap data
		var image = new Image();
		var error = image.LoadBmpFromBuffer(bitmapData);
		if (error != Error.Ok)
		{
			GD.PrintErr("[VideoCall] Failed converting webcam data into something useable ...");
			return null;
		}

		// Resize to fit buffer
		return image;
	}

	private Image imageFromJPG(byte[] data)
	{
		// Create an image from the bitmap data
		var image = new Image();
		var error = image.LoadJpgFromBuffer(data);
		if (error != Error.Ok)
		{
			GD.PrintErr("[VideoCall] Failed converting JPG buffer");
			return null;
		}

		// Resize to fit buffer
		return image;
	}

	private void setLocalFrame(ImageTexture imageTexture, long frameIndex)
	{
		CallDeferred("_setLocalFrame", imageTexture, frameIndex);
	}

	/// <summary>
	/// ⚠️ Call this function with `CallDeferred` to run on a Godot thread! ⚠️
	/// Sets the local frame to the image texture
	/// </summary>
	/// <param name="frameData"></param> <summary>
	/// 
	/// </summary>
	/// <param name="frameData"></param>
	private void _setLocalFrame(ImageTexture imageTexture, long frameIndex)
	{
		localVideo.Texture = imageTexture;

		localFrameLabel.Text = string.Format(localFrameLabelTemplate, frameIndex);
	}

	private void setRemoteFrame(ImageTexture imageTexture, long frameIndex)
	{
		CallDeferred("_setRemoteFrame", imageTexture, frameIndex);
	}

	/// <summary>
	/// ⚠️ Call this function with `CallDeferred` to run on a Godot thread! ⚠️
	/// Sets the local frame to the image texture
	/// </summary>
	/// <param name="frameData"></param> <summary>
	/// 
	/// </summary>
	/// <param name="frameData"></param>
	private void _setRemoteFrame(ImageTexture imageTexture, long frameIndex)
	{
		remoteVideo.Texture = imageTexture;

		remoteFrameLabel.Text = string.Format(remoteFrameLabelTemplate, frameIndex);
	}

	private void sendFrame(Image image, long frameIndex)
	{
		image.Resize(
			240,
			140,
			Image.Interpolation.Nearest
		);
		var jpgBuffer = image.SaveJpgToBuffer();
		var compressed = GZIP.Compress(jpgBuffer);

		var indexToByte = BitConverter.GetBytes(frameIndex);

		var combined = indexToByte.Concat(compressed).ToArray();

		var peer = matchMaker.webRTCConnections.First();
		GD.Print($"[VideoCall] #{frameIndex} Sending: {combined.Length}b");
		peer.Value.SendOnChannelRaw(0, combined);
	}

	private void OnChannelMessageReceivedRaw(string peerUUID, ushort channel, byte[] data)
	{
		var frameIndexBuffer = data.Take(8).ToArray();
		var frameIndex = BitConverter.ToInt64(frameIndexBuffer);

		var compressedImageData = data.Skip(8).Take(data.Length - 8).ToArray();
		var decompressedImageData = GZIP.Decompress(compressedImageData);

		GD.Print($"[VideoCall] #{frameIndex} Received: {decompressedImageData.Length}b");

		var image = imageFromJPG(decompressedImageData);
		if (image != null)
		{
			var imageTexture = ImageTexture.CreateFromImage(image);
			setRemoteFrame(imageTexture, frameIndex);
		}
		else
		{
			GD.PrintErr("Invalid frame received!");
		}
	}
}

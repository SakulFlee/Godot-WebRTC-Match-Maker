using System.Linq;
using System.Threading.Tasks;
using FlashCap;
using Godot;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Complex;

public partial class VideoCall : Node
{
	private static Matrix<double> YUVtoRGBConversionMatrix = Matrix<double>.Build.DenseOfArray(new double[,] { { 0.2126, 0.7152, 0.0722 }, { -0.09991, -0.33609, 0.436 }, { 0.615, -0.55861, -0.05639 } });

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
		matchMaker.OnMessageString += OnChannelMessageReceived;

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


		// // If we are a host, wait for a channel to open and send an initial message
		// matchMaker.OnChannelOpen += (peerUUID, channel) =>
		// {
		// 	if (matchMaker.IsHost)
		// 	{
		// 		GD.Print("[PingPong] Channel opened! Sending initial message ...");
		// 		matchMaker.SendOnChannelString(peerUUID, channel, "Ping!");
		// 	}
		// };
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

	private async Task OnNewFrame(PixelBufferScope pixelBuffer)
	{
		// Update label


		// Get the bitmap data from the pixel buffer.
		// This data can be in various formats (such as YUV or RBG) depending on the capture device.
		// We hope that Godot does supports converting whatever format your webcam is outputting natively.
		// However, in some cases, you may need to convert the given format to something Godot understands before creating an Image from it.
		byte[] bitmapData = pixelBuffer.Buffer.ExtractImage();

		// Create an image from the bitmap data
		var image = new Image();
		var error = image.LoadBmpFromBuffer(bitmapData);
		if (error != Error.Ok)
		{
			GD.PrintErr("[VideoCall] Failed converting webcam data into something useable ...");
			return;
		}

		// Assign the texture.
		// ⚠️ The texture must be set inside a Godot thread! ⚠️
		var imageTexture = ImageTexture.CreateFromImage(image);
		CallDeferred("setLocalFrame", imageTexture, pixelBuffer.Buffer.FrameIndex);
	}

	/// <summary>
	/// ⚠️ Call this function with `CallDeferred` to run on a Godot thread! ⚠️
	/// Sets the local frame to the image texture
	/// </summary>
	/// <param name="frameData"></param> <summary>
	/// 
	/// </summary>
	/// <param name="frameData"></param>
	private void setLocalFrame(ImageTexture imageTexture, long frameIndex)
	{
		localVideo.Texture = imageTexture;

		localFrameLabel.Text = string.Format(localFrameLabelTemplate, frameIndex);
	}

	private void OnChannelMessageReceived(string peerUUID, ushort channel, string message)
	{

	}
}

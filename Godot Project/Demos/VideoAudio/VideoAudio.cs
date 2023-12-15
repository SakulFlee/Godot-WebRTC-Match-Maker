using System.Linq;
using Godot;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorceryMedia.Encoders;

public partial class VideoAudio : Node
{
	private RichTextLabel DebugLabel;
	private Label ConnectionLabel;

	private MatchMaker matchMaker;
	private bool requestSend = false;

	private string debugTemplate;

	private string signalingState = "None";
	private string connectionState = "None";
	private string iceConnectionState = "None";
	private string iceGatheringState = "None";

	private VideoTestPatternSource testPatternSource = new();
	private VideoEncoderEndPoint videoEncoderEndpoint = new();
	private AudioExtrasSource audioSource = new(new AudioEncoder(), new AudioSourceOptions { AudioSource = AudioSourcesEnum.Music });

	private MediaStreamTrack videoTrack;
	private MediaStreamTrack audioTrack;

	private bool connected = false;

	public override void _EnterTree()
	{
		DebugLabel = GetNode<RichTextLabel>("%DebugLabel");
		ConnectionLabel = GetNode<Label>("%ConnectionLabel");

		debugTemplate = DebugLabel.Text;
		DebugLabel.Text = "";

		videoTrack = new(videoEncoderEndpoint.GetVideoSourceFormats(), MediaStreamStatusEnum.SendRecv);
		audioTrack = new(audioSource.GetAudioSourceFormats(), MediaStreamStatusEnum.SendRecv);

		testPatternSource.OnVideoSourceRawSample += videoEncoderEndpoint.ExternalVideoSourceRawSample;

	}

	public override void _Ready()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");
		matchMaker.OnMessageString += ChannelMessageReceived;
		matchMaker.OnMatchMakerUpdate += OnMatchMakerUpdate;
		matchMaker.OnNewConnection += (peerUUID) =>
		{
			matchMaker.webRTCConnections[peerUUID].OnSignalingStateChange += (state) =>
			{
				signalingState = state;
			};
			matchMaker.webRTCConnections[peerUUID].OnConnectionStateChange += async (state) =>
			{
				connectionState = state;

				if (state == "connected")
				{
					connected = true;

					await audioSource.StartAudio();
					await testPatternSource.StartVideo();
				}
				else if (state == "closed")
				{
					await testPatternSource.CloseVideo();
					await audioSource.CloseAudio();
				}
			};
			matchMaker.webRTCConnections[peerUUID].OnICEConnectionStateChange += (state) =>
			{
				iceConnectionState = state;
			};
			matchMaker.webRTCConnections[peerUUID].OnICEGatheringStateChange += (state) =>
			{
				iceGatheringState = state;
			};
		};
		matchMaker.OnNewWebRTCPeer += async peerUUID =>
		{
			var peer = matchMaker.webRTCConnections[peerUUID];

			peer.AddMediaStreamTrack(videoTrack);
			peer.AddMediaStreamTrack(audioTrack);

			videoEncoderEndpoint.OnVideoSourceEncodedSample += peer.SendVideo;
			audioSource.OnAudioSourceEncodedSample += peer.SendAudio;

			peer.OnVideoFormatsNegotiated += () =>
			{
				videoEncoderEndpoint.SetVideoSourceFormat(peer.NegotiatedVideoFormats.First());
			};
			peer.OnAudioFormatsNegotiated += () =>
			{
				audioSource.SetAudioSourceFormat(peer.NegotiatedAudioFormats.First());
			};

			await peer.Initialize();
		};
		matchMaker.OnChannelOpen += (peerUUID, channel) =>
		{
			// TODO
		};
	}

	public override void _Process(double delta)
	{
		if (!requestSend && matchMaker.IsReady())
		{
			var error = matchMaker.SendMatchMakerRequest(new MatchMakerRequest()
			{
				name = "PingPong",
			});
			requestSend = error == Error.Ok;
		}

		if (connected && !ConnectionLabel.Visible)
		{
			ConnectionLabel.Hide();
		}
	}

	private void OnMatchMakerUpdate(uint currentPeerCount, uint requiredPeerCount)
	{
		GD.Print($"Status: {currentPeerCount}/{requiredPeerCount}");
		ConnectionLabel.Text = $"Waiting for players ...\n{currentPeerCount}/{requiredPeerCount}";
	}

	private void ChannelMessageReceived(string peerUUID, ushort channel, string message)
	{
		// TODO
	}
}

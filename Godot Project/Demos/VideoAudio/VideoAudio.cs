using System.Linq;
using Godot;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorceryMedia.Encoders;

public partial class VideoAudio : Node
{
	private MatchMaker matchMaker;

	private VideoTestPatternSource testPatternSource = new();
	private VideoEncoderEndPoint videoEncoderEndpoint = new();
	private AudioExtrasSource audioSource = new(new AudioEncoder(), new AudioSourceOptions { AudioSource = AudioSourcesEnum.Music });

	private MediaStreamTrack videoTrack;
	private MediaStreamTrack audioTrack;

	private bool connected = false;

	public override void _EnterTree()
	{
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
			matchMaker.webRTCConnections[peerUUID].OnConnectionStateChange += async (state) =>
			{
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
		if (matchMaker.IsReady() && !matchMaker.RequestSend)
		{
			matchMaker.SendMatchMakerRequest(new MatchMakerRequest()
			{
				name = "PingPong",
			});
		}
	}

	private void ChannelMessageReceived(string peerUUID, ushort channel, string message)
	{
		// TODO
	}
}

using System.ComponentModel.DataAnnotations;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class WebRTCConnection : Node
{
	[Export]
	public string PeerUUID;

	[Export]
	public bool IsHost;

	[Export]
	public Dictionary TurnAndStunServerConfig;

	public WebRtcPeerConnection peer;   // TODO: Change to private once workaround is no longer needed
	private WebRtcDataChannel chatChannel;

	// TODO: Add support for new data channels _after_ being connected

	[Signal]
	public delegate void HostICECandidateEventHandler(string peerUUID, string mediaId, int index, string name);

	[Signal]
	public delegate void ClientSessionEventHandler(string peerUUID, string type, string sdp);

	public override void _Ready()
	{
		// TODO: Enable once Workaround is no longer needed!
		// peer = new();

		var err = peer.Initialize(new Dictionary()
		{
			{ "iceServers", TurnAndStunServerConfig }
		});
		if (err != Error.Ok)
		{
			GD.PrintErr("Failed to intitialize WebRTC with server config! Configuration may be invalid");

			GetTree().Quit();
			return;
		}

		chatChannel = peer.CreateDataChannel("chat", new Godot.Collections.Dictionary()
		{
			{ "id", 1 },
			{ "negotiated", true },
		});

		peer.SessionDescriptionCreated += OnSessionDescription;
		if (IsHost)
		{
			peer.IceCandidateCreated += OnICECandidate;

			// Create offer and generate ICE candidates
			peer.CreateOffer();
		}
	}

	public override void _Process(double delta)
	{
		peer.Poll();

		if (chatChannel.GetReadyState() == WebRtcDataChannel.ChannelState.Open)
		{
			while (chatChannel.GetAvailablePacketCount() > 0)
			{
				var message = chatChannel.GetPacket().GetStringFromUtf8();
				GD.Print($"[Chat@{PeerUUID}] {message}");

				if (message == "Ping")
				{
					chatChannel.PutPacket("Pong".ToUtf8Buffer());
				}
			}

			if (IsHost)
			{

				var err = chatChannel.PutPacket("Ping".ToUtf8Buffer());
				if (err != Error.Ok)
				{
					GD.PrintErr("Failed sending something!");
				}
			}
		}
	}

	/// <summary>
	/// Should only be called on hosts, not clients.
	/// Hosts send this to clients.
	/// </summary>
	/// <param name="mediaId"></param>
	/// <param name="index"></param>
	/// <param name="name"></param>
	public void OnICECandidate(string mediaId, long index, string name)
	{
		EmitSignal(SignalName.HostICECandidate, PeerUUID, mediaId, index, name);
	}

	/// <summary>
	/// Should be called on the clients and host.
	/// </summary>
	/// <param name="type"></param>
	/// <param name="sdp"></param>
	public void OnSessionDescription(string type, string sdp)
	{
		peer.SetLocalDescription(type, sdp);
		GD.Print($"[WebRTC@{PeerUUID}] Local Session set!");

		EmitSignal(SignalName.ClientSession, PeerUUID, type, sdp);
	}

	public Error SetRemoteSessionDescription(SessionDescriptionResponse response)
	{
		if (PeerUUID != response.uuid)
		{
			GD.PrintErr($"[WebRTCConnection@{PeerUUID}] Attempting to set remote session with non-matching UUID!");
			return Error.Failed;
		}

		peer.SetRemoteDescription(response.type, response.sdp);
		GD.Print($"[WebRTCConnection@{PeerUUID}] Remote Session set!");

		return Error.Ok;
	}

	public Error AddICECandidate(ICECandidateResponse response)
	{
		if (PeerUUID != response.uuid)
		{
			GD.PrintErr($"[WebRTCConnection@{PeerUUID}] Attempting to set remote session with non-matching UUID!");
			return Error.Failed;
		}

		peer.AddIceCandidate(response.mediaId, response.index, response.name);
		GD.Print($"[WebRTCConnection@{PeerUUID}] ICE Candidate added!");

		return Error.Ok;
	}
}

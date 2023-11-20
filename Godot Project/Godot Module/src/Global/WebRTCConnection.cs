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
		var err = peer.Initialize(new Dictionary() {
			{"iceServers", new Dictionary() {
				{"urls", new string[]{
					"turn:calamity-chicken.sakul-flee.de:3478"
				} },
				{"username", "fWfgM4lFGdcniMtkRBiJkwmLcmlFtGNWZgtsYQBpe7AIycUkb77MQy0wztJ-XOunqlKyq1n7T0G2UbEA8LXHN17CWtidDZaGKyfotPXJSPwfweIuJz0YZirKgU_IACJoSUxuYgIG-rxo6DPm24lLOHf0RojVlM8Og-s8GYrjgOe2d9gtXWZux92mhnjeXRCwqJ7dc8JXNLkAYg6Ux8oBNI-lkw2gUenuv1-xLiaoTuEhAOZlZ4dmooAs5XBnR7op5mdD540myoGRjTLZaF0cdqPNx448KbKbRNJp-yKWnsMpSs96eXGdy2rmaQDuYLaKW7ltv9mzkV3bHpTbTLtnXj3a5rQ8xwu8fGuf0ZxVfT38KS_j-YktbulvaoTKdYmKY94QDSHbEJnk6x-cThbZzmJIbw3vYo6WI2oIgYqoBiU0MA1tFrCvsuRPn8vjhmaz7wI9BoneqT2HQmoWbscZLr5TYLScUswf4IxVQIZE1e6FKKaVMKe08Qmf1rTbAKRc"},
				{"credential", "Jp061115KZbB1zk9mPW4pk05gpOltu5TFNjLa5Z5lnaUXfw8glACfUAuLv-EBvabyH6egnUoVX7PUp5V4dlFmx6U797fiffFCtajC4FzIWGJaY7CVQYdqKLaxGU70QR9k4RbxnVE2LXRolgNeRh2sZh0H7BUwByIf6Pk6Z1-u069IYuBOSETaTY9Oc7zkIqOhtTG6knlCYuC0rKVqKbAPybnTCM40-XsBh2cYRhGphO1n05L6pvpikwYLjrQaq3YBYRnlFmvUpGZVarHiLVbmKvkrV4LlYSFricvKWOhev5j6ct8k2hTKuhkdLie-x1cJj7Lofp_EycrU6SKUlY33oscEX0PiZn6IWCkEb-1ChuBZGb_MdnTCfxTYgXoLjvdYGflcftxALJVc-VfgeGGziPvb3Ejf6kUY4UswIbbvSv9FRXIUyMQSkhxKNpmKmLtcCPB1Z9CUHWN2KKA9-73rbeSk6pUO2dwaEIYZZQU4tY5nt67y9-mO_6g3lbjUgXM"},
			}},
		});

		// {
		//     "iceServers": [
		//     		{
		//     			"urls": [ "turn:turn.example.com:3478" ], # One or more TURN servers.
		//     			"username": "a_username", # Optional username for the TURN server.
		//     			"credential": "a_password", # Optional password for the TURN server.
		//     		}
		//     ]
		// }

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

using Godot;
using Godot.Collections;

[GlobalClass]
public partial class MatchMaker : Node
{
	/// <summary>
	/// NOTE: This is currently just a workaround and won't be needed 
	/// anymore in the future!
	/// 
	/// The GDScript containing the workaround for making WebRTC work 
	/// in current Godot versions.
	/// </summary>
	[Export]
	public GDScript WorkaroundScript;

	/// <summary>
	/// The connection string to the Match Maker server.
	/// This should be in the format of:
	/// ws://<ip or domain>:<port>
	/// 
	/// Or, for SSL secured sockets:
	/// wss://<ip or domain>:<port>
	/// </summary>
	[Export]
	public string MatchMakerConnectionString = "ws://127.0.0.1:33333";

	/// <summary>
	/// Expected to be in the same format as <see cref="WebRtcPeerConnection.Initialize"/>.
	/// 
	/// Mainly, a field `urls` is expected with one or more strings (string[]):
	/// 
	/// <code>
	/// {
	/// 	"urls",
	///		[
	/// 		"stun:some-stun-server.com:3478",
	///			"turn:some-turn-server.com:3478"
	/// 	]
	/// }
	/// </code>
	///
	/// Additionally, especially for TURN servers, you want to add 
	/// your credentials here too.
	/// To do this, add a `username` and `credential` field:
	/// 
	/// <code>
	/// {
	///		"urls": [
	///			"stun:some-stun-server.com:3478",
	///			"turn:some-turn-server.com:3478"
	///		],
	///		"username": {
	///			"YOUR USERNAME HERE"
	/// 	},
	/// 	"credential": {
	///			"YOUR CREDENTIALS HERE"
	/// 	}
	/// }
	/// </code>
	/// </summary>
	[Export]
	public Dictionary TurnAndStunServerConfig = new() {
		{
			"urls",
			new string[] {
				"stun:stun.l.google.com:19302"
			}
		}
	};

	private WebSocketPeer peer;

	private Dictionary<string, WebRTCConnection> webRTCConnections;

	[Signal]
	public delegate void ChannelMessageReceivedEventHandler(string peerUUID, string channel, byte[] data);

	public override void _Ready()
	{
		if (WorkaroundScript == null)
		{
			GD.PrintErr("No Workaround Script path set!");
			return;
		}

		peer = new();
		var err = peer.ConnectToUrl(MatchMakerConnectionString);
		if (err != Error.Ok)
		{
			GD.PrintErr($"[MatchMaker] Failed connecting to Match Maker! ({err})");
			return;
		}

		webRTCConnections = new();
	}

	public override void _Process(double delta)
	{
		peer.Poll();
		if (peer.GetReadyState() == WebSocketPeer.State.Open)
		{
			// Read packages if available
			while (peer.GetAvailablePacketCount() > 0)
			{
				var message = peer.GetPacket().GetStringFromUtf8();
				GD.Print("Message: " + message);

				var response = Response.FromJson(message);
				if (response == null)
				{
					GD.PrintErr("[MatchMaker] Invalid JSON received! (parsing failed)");
					GetTree().Quit();
					return;
				}

				if (response.MatchMaking != null)
				{
					foreach (var peerUUID in response.MatchMaking.peers)
					{
						var workaroundScriptInstance = (GodotObject)WorkaroundScript.New();
						var workaroundScriptInner = (WebRtcPeerConnection)workaroundScriptInstance.Get("inner");

						var connection = new WebRTCConnection()
						{
							Name = $"WebRTCConnection@{peerUUID}",
							IsHost = response.MatchMaking.isHost,
							PeerUUID = peerUUID,
							TurnAndStunServerConfig = TurnAndStunServerConfig,
							peer = workaroundScriptInner,
						};
						connection.HostICECandidate += OnHostICECandidate;
						connection.ClientSession += OnClientSession;
						AddChild(connection);

						webRTCConnections.Add(peerUUID, connection);
					}
				}
				else if (response.ICECandidate != null)
				{
					webRTCConnections[response.ICECandidate.uuid].AddICECandidate(response.ICECandidate);
				}
				else if (response.SessionDescription != null)
				{
					webRTCConnections[response.SessionDescription.uuid].SetRemoteSessionDescription(response.SessionDescription);
				}
				else
				{
					GD.PrintErr("[MatchMaker] Invalid JSON received! (no candidate)");
					GetTree().Quit();
					return;
				}
			}
		}
	}

	public bool IsReady()
	{
		return peer.GetReadyState() == WebSocketPeer.State.Open;
	}

	public Error SendRequest(MatchMakingRequest matchMaking)
	{
		if (peer.GetReadyState() != WebSocketPeer.State.Open)
		{
			return Error.Failed;
		}

		var request = new Request()
		{
			MatchMaking = matchMaking,
		};
		var requestJson = request.ToJSON();

		return peer.PutPacket(requestJson.ToUtf8Buffer());
	}

	public void OnHostICECandidate(string peerUUID, string mediaId, int index, string name)
	{
		GD.Print($"[ICECandidate@{peerUUID}] {mediaId} - {index} - {name}");

		var iceCandidateRequest = new ICECandidateRequest()
		{
			uuid = peerUUID,
			mediaId = mediaId,
			index = index,
			name = name,
		};
		var request = new Request()
		{
			ICECandidate = iceCandidateRequest,
		};
		var requestJson = request.ToJSON();

		var err = peer.PutPacket(requestJson.ToUtf8Buffer());
		if (err != Error.Ok)
		{
			GD.PrintErr($"[IceCandidate] Failed to send to Match Maker!");
			return;
		}
	}

	public void OnClientSession(string peerUUID, string type, string sdp)
	{
		GD.Print($"[Session@{peerUUID}]: {type} - {sdp}");

		var sessionDescriptionRequest = new SessionDescriptionRequest()
		{
			uuid = peerUUID,
			type = type,
			sdp = sdp,
		};
		var request = new Request()
		{
			SessionDescription = sessionDescriptionRequest,
		};
		var requestJson = request.ToJSON();

		var err = peer.PutPacket(requestJson.ToUtf8Buffer());
		if (err != Error.Ok)
		{
			GD.PrintErr($"[SessionDescription] Failed to send to Match Maker!");
			return;
		}
	}

	private void OnChannelMessageReceived(string peerUUID, string channel, byte[] data)
	{
		EmitSignal(SignalName.ChannelMessageReceived, peerUUID, channel, data);
	}

	public Error SendMessageOnChannel(string peerUUID, string channel, byte[] data)
	{
		return webRTCConnections[peerUUID].SendMessageOnChannel(channel, data);
	}
}

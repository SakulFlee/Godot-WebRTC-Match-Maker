using System.Dynamic;
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

	public WebSocketPeer peer { get; private set; }

	public Dictionary<string, WebRTCConnection> webRTCConnections { get; private set; }

	public System.Collections.Generic.LinkedList<(string, long, string)> LocalICECandidates
	{
		get; private set;
	} = new();
	public System.Collections.Generic.LinkedList<(string, long, string)> RemoteICECandidates { get; private set; } = new();

	public (string, string) LocalSession { get; private set; }
	public (string, string) RemoteSession { get; private set; }

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
						connection.ChannelMessageReceived += OnChannelMessageReceived;

						webRTCConnections.Add(peerUUID, connection);
					}
				}
				else if (response.ICECandidate != null)
				{
					webRTCConnections[response.ICECandidate.uuid].AddICECandidate(response.ICECandidate);

					RemoteICECandidates.AddLast((response.ICECandidate.mediaId, response.ICECandidate.index, response.ICECandidate.name));
				}
				else if (response.SessionDescription != null)
				{
					webRTCConnections[response.SessionDescription.uuid].SetRemoteSessionDescription(response.SessionDescription);

					RemoteSession = (response.SessionDescription.type, response.SessionDescription.sdp);
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
		LocalICECandidates.AddLast((mediaId, index, name));

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
		LocalSession = (type, sdp);

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

	public Error BroadcastMessageOnChannel(string channel, byte[] data)
	{
		var anyErrors = false;
		foreach (var connection in webRTCConnections)
		{
			var err = connection.Value.SendMessageOnChannel(channel, data);
			if (err != Error.Ok)
			{
				GD.PrintErr($"Failed broadcasting message: {err}");
				anyErrors = true;
			}
		}

		if (anyErrors)
		{
			return Error.Failed;
		}
		else
		{
			return Error.Ok;
		}
	}

	public bool IsChannelReady(string peerUUID, string channel)
	{
		return webRTCConnections[peerUUID].IsChannelReady(channel);
	}
}
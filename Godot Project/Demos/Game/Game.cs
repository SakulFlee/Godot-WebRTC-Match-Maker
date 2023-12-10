using System;
using System.Collections.Generic;
using Godot;

public partial class Game : Node
{
	[Export]
	public PackedScene PlayerScene;

	[Export]
	public float PlayerSpeed = 400.0f;

	private Dictionary<string, Player> players = [];

	private RichTextLabel DebugLabel;
	private Control ConnectionPanel;
	private Label ConnectionLabel;

	private MatchMaker matchMaker;
	private bool requestSend = false;

	private string debugTemplate;

	private string signalingState = "None";
	private string connectionState = "None";
	private string iceConnectionState = "None";
	private string iceGatheringState = "None";

	private bool connected = false;
	private bool addPlayerPacketSend = false;

	public override void _EnterTree()
	{
		DebugLabel = GetNode<RichTextLabel>("%DebugLabel");
		ConnectionLabel = GetNode<Label>("%ConnectionLabel");
		ConnectionPanel = GetNode<Control>("%ConnectionPanel");

		debugTemplate = DebugLabel.Text;
		DebugLabel.Text = "";
	}

	public override void _Ready()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");
		matchMaker.OnMessageString += OnChannelMessageReceived;
		matchMaker.OnNewConnection += (peerUUID) =>
		{
			matchMaker.webRTCConnections[peerUUID].OnSignalingStateChange += (state) =>
			{
				signalingState = state;
			};
			matchMaker.webRTCConnections[peerUUID].OnConnectionStateChange += (state) =>
			{
				connectionState = state;

				if (state == "connected")
				{
					connected = true;
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
			matchMaker.OnChannelOpen += (peerUUID, channel) =>
			{
				if (peerUUID == matchMaker.HostUUID || matchMaker.IsHost)
				{
					SendGamePacketToHost(channel, new GamePacket(GamePacketType.AddPlayer, new GamePacketAddPlayer()
					{
						Label = matchMaker.OwnUUID,
					}));
				}
			};
		};
		UpdateLabel();
	}

	public override void _Process(double delta)
	{
		if (!requestSend && matchMaker.IsReady())
		{
			var error = matchMaker.SendMatchMakingRequest(new MatchMakingRequest()
			{
				name = "Chat",
			});
			requestSend = error == Error.Ok;
		}
		UpdateLabel();
	}

	private void UpdateLabel()
	{
		var peersString = "";
		foreach (var (peerUUID, _) in matchMaker.webRTCConnections)
		{
			peersString += $"- {peerUUID}";
		}

		DebugLabel.Text = string.Format(debugTemplate, new[] {
			signalingState,
			connectionState,
			iceConnectionState,
			iceGatheringState,
			matchMaker.OwnUUID,
			matchMaker.HostUUID,
			matchMaker.IsHost ? "yes" : "no",
			peersString
		});

		if (connected && (ConnectionLabel.Visible || ConnectionPanel.Visible))
		{
			ConnectionLabel.Visible = false;
			ConnectionPanel.Visible = false;
		}
	}

	private void SendGamePacketToHost(ushort channel, GamePacket gamePacket)
	{
		SendGamePacketToPeer(matchMaker.HostUUID, channel, gamePacket);
	}

	private void SendGamePacketToHost(GamePacket gamePacket)
	{
		SendGamePacketToHost(WebRTCPeer.MAIN_CHANNEL_ID, gamePacket);
	}

	private void SendGamePacketToPeer(string peerUUID, ushort channel, GamePacket gamePacket)
	{
		if (peerUUID == matchMaker.OwnUUID)
		{
			// If it's our own UUID, just pass it
			OnGamePacketReceived(peerUUID, channel, gamePacket);
		}
		else
		{
			// If not, send it to the peer
			var json = gamePacket.ToJSON();
			matchMaker.webRTCConnections[peerUUID].SendOnChannel(channel, json);
		}
	}

	private void SendGamePacketToPeer(string peerUUID, GamePacket gamePacket)
	{
		SendGamePacketToPeer(peerUUID, WebRTCPeer.MAIN_CHANNEL_ID, gamePacket);
	}

	private void SendGamePacketBroadcast(ushort channel, GamePacket gamePacket)
	{
		var json = gamePacket.ToJSON();
		foreach (var (_, connection) in matchMaker.webRTCConnections)
		{
			connection.SendOnChannel(channel, json);
		}
	}

	private void SendGamePacketBroadcast(GamePacket gamePacket)
	{
		SendGamePacketBroadcast(WebRTCPeer.MAIN_CHANNEL_ID, gamePacket);
	}

	private void SendGamePacketBroadcastIncludingSelf(ushort channel, GamePacket gamePacket)
	{
		SendGamePacketBroadcast(channel, gamePacket);

		OnGamePacketReceived(matchMaker.OwnUUID, channel, gamePacket);
	}

	private void SendGamePacketBroadcastIncludingSelf(GamePacket gamePacket)
	{
		SendGamePacketBroadcastIncludingSelf(WebRTCPeer.MAIN_CHANNEL_ID, gamePacket);
	}

	private void OnChannelMessageReceived(string peerUUID, ushort channel, string json)
	{
		var gamePacket = GamePacket.FromJSON(json);

		OnGamePacketReceived(peerUUID, channel, gamePacket);
	}

	private void OnGamePacketReceived(string peerUUID, ushort channel, GamePacket gamePacket)
	{
		switch (gamePacket.Type)
		{
			case GamePacketType.AddPlayer:
				HandleAddPlayer(peerUUID, gamePacket.InnerAs<GamePacketAddPlayer>());
				break;
			case GamePacketType.Input:
				if (!matchMaker.IsHost)
				{
					GD.PrintErr($"[Game] Received {GamePacketType.Input} as non-host!");
					return;
				}

				HandleInput(peerUUID, gamePacket.InnerAs<GamePacketInput>());
				break;
			case GamePacketType.PlayerMove:
				HandlePlayerMove(peerUUID, gamePacket.InnerAs<GamePacketPlayerMove>());
				break;
			default:
				break;
		}
	}

	/// <summary>
	/// Called when a <see cref="GamePacketType.AddPlayer"/> packet is received.
	/// </summary>
	/// <param name="peerUUID">Where this packet came from</param>
	/// <param name="packet">The <see cref="GamePacket"/> send</param>
	private void HandleAddPlayer(string peerUUID, GamePacketAddPlayer packet)
	{
		var isUs = packet.Label == matchMaker.OwnUUID;

		// Instantiate a new player
		var newPlayer = PlayerScene.Instantiate<Player>();
		newPlayer.PeerUUID = packet.Label;
		newPlayer.IsControlledByUs = isUs;
		newPlayer.IsHost = matchMaker.IsHost;

		if (matchMaker.IsHost)
		{
			// If host, set a random position
			var random = new Random();
			var x = random.Next(-250, 250);
			var y = random.Next(-250, 250);
			newPlayer.Position = new Vector2(x, y);
		}
		else
		{
			// If client, set the location provided
			newPlayer.Position = packet.Position;
		}

		// Add listener for input change event
		// Applies to ALL peers, given it is OUR (as in controlled by us) player.
		if (isUs)
		{
			newPlayer.OnInputChanged += (inputVector) =>
			{
				SendGamePacketToHost(new GamePacket(GamePacketType.Input, new GamePacketInput()
				{
					InputVector = inputVector,
				}));
			};
		}

		// The host needs to synchronize the position of each player to every peer
		if (matchMaker.IsHost)
		{
			newPlayer.OnPositionChanged += (peerUUID, position) =>
			{
				SendGamePacketBroadcast(new GamePacket(GamePacketType.PlayerMove, new GamePacketPlayerMove()
				{
					PeerUUID = peerUUID,
					Position = position,
				}));
			};
		}

		players.Add(packet.Label, newPlayer);
		AddChild(newPlayer);

		// If we are a host, send the packet to every peer
		if (matchMaker.IsHost)
		{
			SendGamePacketBroadcast(new GamePacket(GamePacketType.AddPlayer, new GamePacketAddPlayer
			{
				Label = peerUUID,
				Position = newPlayer.Position,
			}));
		}
	}

	private void HandleInput(string peerUUID, GamePacketInput packet)
	{
		var inputVector = packet.InputVector.Clamp(-Vector2.One, Vector2.One);

		var player = players[peerUUID];
		player.ApplyInputVector(inputVector);
	}

	private void HandlePlayerMove(string peerUUID, GamePacketPlayerMove packet)
	{
		if (peerUUID != matchMaker.HostUUID)
		{
			GD.PrintErr($"[Game] {GamePacketType.PlayerMove} received from non-host!");
			return;
		}

		var player = players[packet.PeerUUID];
		player.ApplyPosition(packet.Position);
	}
}

using System.Collections.Generic;
using Godot;

public partial class Game : Node
{
	[Export]
	private PackedScene playerScene;

	private CharacterBody2D ownPlayer;
	private Dictionary<string, CharacterBody2D> players = new();

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
					var packet = GamePacket.MakeAddPlayerPacket(matchMaker.OwnUUID);
					SendGamePacketToHost(channel, packet);
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
				name = "Test",
			});
			requestSend = error == Error.Ok;
		}
		UpdateLabel();

		if (ownPlayer != null && Input.IsAnythingPressed())
		{
			var inputDirection = Input.GetVector("Move Left", "Move Right", "Move Forward", "Move Backward");

			ownPlayer.Velocity = inputDirection * 400;
			ownPlayer.MoveAndSlide();
		}
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

	/// <summary>
	/// Called when a <see cref="GamePacketType.AddPlayer"/> packet is received.
	/// </summary>
	/// <param name="gamePacket"></param>
	private void HandleAddPlayer(string peerUUID, GamePacket gamePacket)
	{
		// Instantiate a new player
		var newPlayer = playerScene.Instantiate<CharacterBody2D>();
		newPlayer.Name = $"Player#{gamePacket.Inner}";

		// Set the player label to the UUID
		var idNode = newPlayer.GetNode<RichTextLabel>("VBoxContainer/ID");
		idNode.Text = $"{{{gamePacket.Inner}}}";

		AddChild(newPlayer);

		// If the peer UUID is our own, add this also as our own player that we can control
		if (gamePacket.Inner == matchMaker.OwnUUID)
		{
			ownPlayer = newPlayer;
		}

		if (matchMaker.IsHost)
		{
			SendGamePacketBroadcast(GamePacket.MakeAddPlayerPacket(peerUUID));
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

	private void OnGamePacketReceived(string peerUUID, ushort channel, GamePacket gamePacket)
	{
		GD.Print($"[{peerUUID}@{channel}]: GamePacket -> {gamePacket.Type}: {gamePacket.Inner}");

		switch (gamePacket.Type)
		{
			case GamePacketType.AddPlayer:
				HandleAddPlayer(peerUUID, gamePacket);
				break;
			default:
				break;
		}
	}

	private void OnChannelMessageReceived(string peerUUID, ushort channel, string message)
	{
		var gamePacket = GamePacket.FromJSON(message);

		OnGamePacketReceived(peerUUID, channel, gamePacket);
	}
}

using System;
using System.Collections.Generic;
using Godot;

public partial class Game : Node
{
	#region Exports
	[Export]
	public PackedScene PlayerScene;

	[Export]
	public float PlayerSpeed = 400.0f;
	#endregion

	#region Variables
	private MatchMaker matchMaker;

	private Dictionary<string, Player> players = [];
	#endregion

	public override void _EnterTree()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");

		GetNode<DebugPanel>("%DebugPanel").matchMaker = matchMaker;
		GetNode<ConnectionPanel>("%ConnectionPanel").matchMaker = matchMaker;
	}

	public override void _Ready()
	{
		matchMaker.OnMessageString += OnChannelMessageReceived;

		matchMaker.OnNewConnection += (peerUUID) =>
		{
			matchMaker.OnChannelOpen += (peerUUID, channel) =>
			{
				if (matchMaker.IsHost)
				{
					// If the player list is empty, spawn ourself first
					if (players.Count == 0)
					{
						var _ = spawnPlayerRandomLocation(matchMaker.HostUUID);
					}

					// Create new player
					var newPlayer = spawnPlayerRandomLocation(peerUUID);

					// Send any existing players
					foreach (var (playerUUID, player) in players)
					{
						SendGamePacketToPeer(peerUUID, new GamePacket(GamePacketType.Player, new GamePacketPlayer()
						{
							PlayerUUID = playerUUID,
							Position = player.Position,
						}));
					}
				}
			};
		};
	}

	public override void _Process(double delta)
	{
		if (matchMaker.IsReady() && !matchMaker.RequestSend)
		{
			matchMaker.SendMatchMakerRequest(new MatchMakerRequest()
			{
				name = "Game",
			});
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

	private void OnGamePacketReceived(string peerUUID, ushort _, GamePacket gamePacket)
	{
		switch (gamePacket.Type)
		{
			case GamePacketType.Player:
				HandlePlayerGamePacket(peerUUID, gamePacket.InnerAs<GamePacketPlayer>());
				break;
			case GamePacketType.Input:
				if (!matchMaker.IsHost)
				{
					GD.PrintErr($"[Game] Received {GamePacketType.Input} as non-host!");
					return;
				}

				HandleInputGamePacket(peerUUID, gamePacket.InnerAs<GamePacketInput>());
				break;
			default:
				break;
		}
	}

	private Player spawnPlayerRandomLocation(string playerUUID)
	{
		var random = new Random();
		var x = random.Next(-250, 250);
		var y = random.Next(-250, 250);
		var position = new Vector2(x, y);

		return spawnPlayer(playerUUID, position);
	}

	/// <summary>
	/// Called when a <see cref="GamePacketType.AddPlayer"/> packet is received.
	/// </summary>
	/// <param name="peerUUID">Where this packet came from</param>
	/// <param name="packet">The <see cref="GamePacket"/> send</param>
	private Player spawnPlayer(string peerUUID, Vector2 position)
	{
		var isUs = peerUUID == matchMaker.OwnUUID;

		// Instantiate a new player
		var newPlayer = PlayerScene.Instantiate<Player>();
		newPlayer.PeerUUID = peerUUID;
		newPlayer.IsControlledByUs = isUs;
		newPlayer.IsHost = matchMaker.IsHost;
		newPlayer.Position = position;

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
				SendGamePacketBroadcast(new GamePacket(GamePacketType.Player, new GamePacketPlayer()
				{
					PlayerUUID = peerUUID,
					Position = position,
				}));
			};
		}

		players.Add(peerUUID, newPlayer);
		AddChild(newPlayer);

		// If we are a host, send the packet to every peer
		if (matchMaker.IsHost)
		{
			SendGamePacketBroadcast(new GamePacket(GamePacketType.Player, new GamePacketPlayer()
			{
				PlayerUUID = peerUUID,
				Position = newPlayer.Position,
			}));
		}

		return newPlayer;
	}

	private void HandleInputGamePacket(string peerUUID, GamePacketInput packet)
	{
		var inputVector = packet.InputVector.Clamp(-Vector2.One, Vector2.One);

		var player = players[peerUUID];
		player.ApplyInputVector(inputVector);
	}

	private void HandlePlayerGamePacket(string peerUUID, GamePacketPlayer packet)
	{
		if (peerUUID != matchMaker.HostUUID)
		{
			GD.PrintErr($"[Game] Received game packet '{GamePacketType.Player}' from non-host!");
			return;
		}

		Player player;
		if (!players.TryGetValue(packet.PlayerUUID, out player))
		{
			// Spawn player as it doesn't exist
			player = spawnPlayer(packet.PlayerUUID, packet.Position);
		}

		player.ApplyPosition(packet.Position);
	}
}

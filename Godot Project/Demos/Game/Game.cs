using Godot;

public partial class Game : Node
{
	[Export]
	private PackedScene playerScene;

	private GodotObject player;

	private RichTextLabel DebugLabel;
	private Label ConnectionLabel;

	private MatchMaker matchMaker;
	private bool requestSend = false;

	private string debugTemplate;

	private string signalingState = "None";
	private string connectionState = "None";
	private string iceConnectionState = "None";
	private string iceGatheringState = "None";

	private bool connected = false;

	public override void _EnterTree()
	{
		DebugLabel = GetNode<RichTextLabel>("%DebugLabel");
		ConnectionLabel = GetNode<Label>("%ConnectionLabel");

		debugTemplate = DebugLabel.Text;
		DebugLabel.Text = "";
	}

	public override void _Ready()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");
		matchMaker.OnMessageString += ChannelMessageReceived;
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

		if (connected && player == null)
		{
			AddPlayer(matchMaker.OwnUUID);
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

		if (connected && ConnectionLabel.Visible)
		{
			ConnectionLabel.Visible = false;
		}
	}

	private void SendMessage(string message)
	{
		foreach (var (_, value) in matchMaker.webRTCConnections)
		{
			value.SendOnChannel(WebRTCPeer.MAIN_CHANNEL_ID, message);
		}

		ChannelMessageReceived(matchMaker.OwnUUID, WebRTCPeer.MAIN_CHANNEL_ID, message);
	}

	private void AddPlayer(string peerUUID)
	{
		var player = playerScene.Instantiate();
		player.Name = $"Player#{peerUUID}";
		var idNode = player.GetNode<Label>("VBoxContainer/ID");
		idNode.Text = peerUUID;

		AddChild(player);
	}

	private void ChannelMessageReceived(string peerUUID, ushort channel, string message)
	{
		var gamePacket = GamePacket.FromJSON(message);

	}
}

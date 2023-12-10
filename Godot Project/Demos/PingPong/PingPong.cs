using Godot;

public partial class PingPong : Node
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

	private bool connected = false;
	private bool connectionLabelChanged = false;
	private uint sendPingCounter = 0;
	private uint sendPongCounter = 0;
	private uint receivedPingCounter = 0;
	private uint receivedPongCounter = 0;

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
		matchMaker.OnChannelOpen += (peerUUID, channel) =>
		{
			if (matchMaker.IsHost)
			{
				GD.Print("[PingPong] Sending PING ...");
				matchMaker.SendOnChannelString(peerUUID, channel, "Ping!");
			}
		};

		UpdateLabel();
	}

	public override void _Process(double delta)
	{
		if (!requestSend && matchMaker.IsReady())
		{
			var error = matchMaker.SendMatchMakingRequest(new MatchMakingRequest()
			{
				name = "PingPong",
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

		if (connected)
		{
			ConnectionLabel.Text = $@"Send           Pings: {sendPingCounter} | Pongs: {sendPongCounter}
Received    Pings: {receivedPingCounter} | Pongs: {receivedPongCounter}";
		}
	}

	private void ChannelMessageReceived(string peerUUID, ushort channel, string message)
	{
		// Send back Pings and Pongs!
		if (message == "Ping!")
		{
			matchMaker.SendOnChannelString(peerUUID, channel, "Pong!");

			receivedPingCounter++;
			sendPongCounter++;
		}
		else if (message == "Pong!")
		{
			matchMaker.SendOnChannelString(peerUUID, channel, "Ping!");

			receivedPongCounter++;
			sendPingCounter++;
		}
		else
		{
			GD.PrintErr("Invalid ping/pong received!");
		}
	}
}

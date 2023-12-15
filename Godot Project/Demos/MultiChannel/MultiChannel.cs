using System.Linq;
using Godot;

public partial class MultiChannel : Node
{
	[Export]
	public string PayloadMessage = "Hello, World!";

	private RichTextLabel DebugLabel;
	private Label ConnectionLabel;
	private Panel ConnectionLabelPanel;

	private MatchMaker matchMaker;
	private bool requestSend = false;

	private string debugTemplate;

	private string signalingState = "None";
	private string connectionState = "None";
	private string iceConnectionState = "None";
	private string iceGatheringState = "None";

	private bool connected = false;

	private ItemList channelList;
	private RichTextLabel logBox;

	public override void _EnterTree()
	{
		DebugLabel = GetNode<RichTextLabel>("%DebugLabel");
		ConnectionLabel = GetNode<Label>("Control/ConnectionLabelPanel/CenterContainer/ConnectionLabel");
		ConnectionLabelPanel = GetNode<Panel>("Control/ConnectionLabelPanel");

		debugTemplate = DebugLabel.Text;
		DebugLabel.Text = "";

		// ---

		channelList = GetNode<ItemList>("Panel/ChannelAndLog/LeftVBox/ChannelList");
		logBox = GetNode<RichTextLabel>("Panel/ChannelAndLog/RightVBox/Panel/LogBox");
	}

	public override void _Ready()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");
		matchMaker.OnMessageString += ChannelMessageReceived;
		matchMaker.OnMatchMakerUpdate += OnMatchMakerUpdate;
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
		matchMaker.OnChannelOpen += (peerUUID, channelId) =>
		{
			channelList.AddItem(matchMaker.webRTCConnections[peerUUID].GetChannelLabel(channelId));
		};

		UpdateLabel();
	}

	public override void _Process(double delta)
	{
		if (!requestSend && matchMaker.IsReady())
		{
			var error = matchMaker.SendMatchMakerRequest(new MatchMakerRequest()
			{
				name = "MultiChannel",
			});
			requestSend = error == Error.Ok;
		}

		UpdateLabel();
	}

	private void OnMatchMakerUpdate(uint currentPeerCount, uint requiredPeerCount)
	{
		GD.Print($"Status: {currentPeerCount}/{requiredPeerCount}");
		ConnectionLabel.Text = $"Waiting for players ...\n{currentPeerCount}/{requiredPeerCount}";
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

		if (connected && ConnectionLabelPanel.Visible)
		{
			ConnectionLabelPanel.Hide();
		}
	}

	private void ChannelMessageReceived(string peerUUID, ushort channel, string message)
	{
		GD.Print($"Received a message from peer {peerUUID}@{channel}: {message}");

		if (message == PayloadMessage)
		{
			matchMaker.SendOnChannelString(peerUUID, channel, $"Received your message '{message}' on channel #{channel} from {peerUUID}!");
		}
		else
		{
			appendLog($"[b]Reply:[/b] {message}");
		}
	}

	public void OnChannelSelected(int index)
	{
		if (logBox.Text.Length > 0)
		{
			appendLog("[center]---[/center]");
		}

		var clickedChannel = channelList.GetItemText(index);
		appendLog($"[b]Channel selected:[/b] {clickedChannel}");

		var firstPeer = matchMaker.webRTCConnections.First().Value;

		var channelId = firstPeer.GetChannelID(clickedChannel);
		appendLog($"[b]Internal channel ID:[/b] {channelId}");

		appendLog($"[b]Sending message:[/b] {PayloadMessage}");
		firstPeer.SendOnChannel(channelId, PayloadMessage);
		appendLog("[b]Message send![/b]");
		appendLog("[b]Awaiting response ...[/b]");
	}

	private void appendLog(string toBeAppended)
	{
		if (logBox.Text.Length > 0)
		{
			logBox.Text = $"{logBox.Text}\n{toBeAppended}";
		}
		else
		{
			logBox.Text = toBeAppended;
		}
	}
}

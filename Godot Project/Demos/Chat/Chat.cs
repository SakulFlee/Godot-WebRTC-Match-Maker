using System.Threading.Channels;
using Godot;

public partial class Chat : Node
{
	private RichTextLabel DebugLabel;
	private Label ConnectionLabel;
	private RichTextLabel ChatLabel;
	private TextEdit ChatTextEdit;

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
		ChatLabel = GetNode<RichTextLabel>("%ChatLabel");
		ChatTextEdit = GetNode<TextEdit>("%ChatTextEdit");

		ChatLabel.Text = "";

		debugTemplate = DebugLabel.Text;
		DebugLabel.Text = "";
	}

	public override void _Ready()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");
		matchMaker.OnMessageString += ChannelMessageReceived;
		matchMaker.OnMatchMakingUpdate += OnMatchMakingUpdate;
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
				name = "Chat",
			});
			requestSend = error == Error.Ok;
		}

		UpdateLabel();
	}

	private void OnMatchMakingUpdate(uint currentPeerCount, uint requiredPeerCount)
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

	private void OnSendButtonPressed()
	{
		var text = ChatTextEdit.Text.Trim();
		if (text.Length > 0)
		{
			SendMessage(text);
		}

		ChatTextEdit.Text = "";
	}

	private void OnChatTextEditChanged()
	{
		var text = ChatTextEdit.Text;
		if (text.Length > 0 && text.EndsWith("\n"))
		{
			text = text.Substr(0, text.Length - 1);
			SendMessage(text);

			ChatTextEdit.Text = "";
		}
	}

	private void ChannelMessageReceived(string peerUUID, ushort channel, string message)
	{
		var pre = "";
		if (ChatLabel.Text != "")
		{
			pre = "\n";
		}
		ChatLabel.Text += $"{pre}[[b][color=red]{peerUUID}[/color]@[color=blue]{channel}[/color][/b]] {message}";
	}
}

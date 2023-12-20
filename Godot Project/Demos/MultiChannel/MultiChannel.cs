using System.Linq;
using Godot;

public partial class MultiChannel : Node
{
	[Export]
	public string PayloadMessage = "Hello, World!";

	private MatchMaker matchMaker;

	private ItemList peerList;
	private string selectedPeer;

	private ItemList channelList;
	private RichTextLabel logBox;

	public override void _EnterTree()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");

		GetNode<DebugPanel>("%DebugPanel").matchMaker = matchMaker;
		GetNode<ConnectionPanel>("%ConnectionPanel").matchMaker = matchMaker;

		peerList = GetNode<ItemList>("%PeerList");
		channelList = GetNode<ItemList>("%ChannelList");
		logBox = GetNode<RichTextLabel>("%LogBox");
	}

	public override void _Ready()
	{
		matchMaker.OnMessageString += ChannelMessageReceived;

		matchMaker.OnNewConnection += (peerUUID) =>
		{
			peerList.AddItem(peerUUID);
		};

		matchMaker.OnChannelOpen += (peerUUID, channel) =>
		{
			GD.Print($"!!! {peerUUID} @ {channel} opened!");
		};
	}

	public override void _Process(double delta)
	{
		if (matchMaker.IsReady() && !matchMaker.RequestSend)
		{
			matchMaker.SendMatchMakerRequest(new MatchMakerRequest()
			{
				name = "MultiChannel",
			});
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

	public void OnPeerSelected(int index)
	{
		var clickedPeer = peerList.GetItemText(index);
		selectedPeer = clickedPeer;

		// Clear the channel list
		channelList.Clear();

		var peer = matchMaker.webRTCConnections[clickedPeer];
		foreach (string channelName in peer.DataChannels)
		{
			channelList.AddItem(channelName);
		}
	}

	public void OnChannelSelected(int index)
	{
		if (logBox.Text.Length > 0)
		{
			appendLog("[center]---[/center]");
		}

		appendLog($"[b]Peer selected:[/b] {selectedPeer}");

		var clickedChannel = channelList.GetItemText(index);
		appendLog($"[b]Channel selected:[/b] {clickedChannel}");

		var peer = matchMaker.webRTCConnections[selectedPeer];

		var channelId = peer.GetChannelID(clickedChannel);
		appendLog($"[b]Internal channel ID:[/b] {channelId}");

		appendLog($"[b]Sending message:[/b] {PayloadMessage}");
		peer.SendOnChannel(channelId, PayloadMessage);
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

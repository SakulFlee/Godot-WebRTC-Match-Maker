using System.Linq;
using Godot;

public partial class MultiChannel : Node
{
	[Export]
	public string PayloadMessage = "Hello, World!";

	private MatchMaker matchMaker;

	private ItemList channelList;
	private RichTextLabel logBox;

	public override void _EnterTree()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");

		GetNode<DebugPanel>("%DebugPanel").matchMaker = matchMaker;
		GetNode<ConnectionPanel>("%ConnectionPanel").matchMaker = matchMaker;

		channelList = GetNode<ItemList>("%ChannelList");
		logBox = GetNode<RichTextLabel>("%LogBox");
	}

	public override void _Ready()
	{
		matchMaker.OnMessageString += ChannelMessageReceived;

		matchMaker.OnChannelOpen += (peerUUID, channelId) =>
		{
			channelList.AddItem(matchMaker.webRTCConnections[peerUUID].GetChannelLabel(channelId));
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

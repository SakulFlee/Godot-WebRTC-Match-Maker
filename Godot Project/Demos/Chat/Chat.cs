using Godot;

public partial class Chat : Node
{
	private MatchMaker matchMaker;

	private RichTextLabel chatBox;
	private TextEdit chatMessageField;

	public override void _EnterTree()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");

		GetNode<DebugPanel>("%DebugPanel").matchMaker = matchMaker;
		GetNode<ConnectionPanel>("%ConnectionPanel").matchMaker = matchMaker;

		chatBox = GetNode<RichTextLabel>("%ChatBox");
		chatMessageField = GetNode<TextEdit>("%ChatMessageField");
	}

	public override void _Ready()
	{
		matchMaker.OnMessageString += ChannelMessageReceived;
	}

	public override void _Process(double delta)
	{
		if (matchMaker.IsReady() && !matchMaker.RequestSend)
		{
			matchMaker.SendMatchMakerRequest(new MatchMakerRequest()
			{
				name = "Chat",
			});
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
		var text = chatMessageField.Text.Trim();
		if (text.Length > 0)
		{
			SendMessage(text);
		}

		chatMessageField.Text = "";
	}

	private void OnChatTextEditChanged()
	{
		var text = chatMessageField.Text;
		if (text.Length > 0 && text.EndsWith("\n"))
		{
			text = text.Substr(0, text.Length - 1);
			SendMessage(text);

			chatMessageField.Text = "";
		}
	}

	private void ChannelMessageReceived(string peerUUID, ushort channel, string message)
	{
		var pre = "";
		if (chatBox.Text != "")
		{
			pre = "\n";
		}

		var color = peerUUID == matchMaker.OwnUUID ? "greenyellow" : "aqua";
		chatBox.Text += $"{pre}[[b][color={color}]{peerUUID}[/color]@[color=blue]{channel}[/color][/b]] {message}";
	}
}

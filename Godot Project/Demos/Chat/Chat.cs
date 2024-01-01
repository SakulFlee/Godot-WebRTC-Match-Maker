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
		var packet = new ChatMessagePacket
		{
			fromUUID = matchMaker.OwnUUID,
			channel = WebRTCPeer.MAIN_CHANNEL_ID,
			messsage = message,
		};
		var json = packet.ToJSON();

		if (matchMaker.IsHost)
		{
			// The message came from us. Add it directly.
			ChannelMessageReceived(matchMaker.OwnUUID, WebRTCPeer.MAIN_CHANNEL_ID, json);
		}
		else
		{
			matchMaker.SendOnChannelString(matchMaker.HostUUID, WebRTCPeer.MAIN_CHANNEL_ID, json);
		}
	}

	private void OnSendButtonPressed()
	{
		var text = chatMessageField.Text.Trim();
		if (text.Length > 0)
		{
			SendMessage(text);
			chatMessageField.Text = "";
		}
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
		var messagePacket = ChatMessagePacket.FromJSON(message);

		var color = messagePacket.fromUUID == matchMaker.OwnUUID ? "greenyellow" : "aqua";
		chatBox.Text += $"\n[[b][color={color}]{messagePacket.fromUUID}[/color]@[color=blue]{messagePacket.channel}[/color][/b]] {messagePacket.messsage}";

		// If we are the host, send the message to everyone
		if (matchMaker.IsHost)
		{
			foreach (var (id, peer) in matchMaker.webRTCConnections)
			{
				// Ignore host, we already got that message ...
				if (id != matchMaker.HostUUID)
				{
					peer.SendOnChannel(channel, message);
				}
			}
		}
	}
}

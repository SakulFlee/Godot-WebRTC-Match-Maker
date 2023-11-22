using Godot;

public partial class Main : Node
{
	private MatchMaker matchMaker;
	private bool requestSend = false;

	public override void _Ready()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");
		matchMaker.ChannelMessageReceived += ChannelMessageReceived;
	}

	public override void _Process(double delta)
	{
		if (!requestSend && matchMaker.IsReady())
		{
			var error = matchMaker.SendRequest(new MatchMakingRequest()
			{
				name = "Test",
			});
			requestSend = error == Error.Ok;
		}
	}

	private void ChannelMessageReceived(string peerUUID, string channel, byte[] data)
	{
		var message = data.GetStringFromUtf8();
		GD.Print($"Message received: {message}");

		matchMaker.SendMessageOnChannel(peerUUID, channel, "Pong!".ToUtf8Buffer());
	}
}

using Godot;

public partial class HighLevel : Node
{
	private MatchMaker matchMaker;

	public override void _EnterTree()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");

		GetNode<DebugPanel>("%DebugPanel").matchMaker = matchMaker;
		GetNode<ConnectionPanel>("%ConnectionPanel").matchMaker = matchMaker;
	}

	public override void _Ready()
	{
		matchMaker.OnMessageString += OnChannelMessageReceived;

		// If we are a host, wait for a channel to open and send an initial message
		matchMaker.OnChannelOpen += (peerUUID, channel) =>
		{
			if (matchMaker.IsHost)
			{
				GD.Print("[HighLevel] Channel opened! Sending initial message ...");
				matchMaker.SendOnChannelString(peerUUID, channel, "Ping!");
			}
		};
	}

	public override void _Process(double delta)
	{
		if (matchMaker.IsReady() && !matchMaker.RequestSend)
		{
			matchMaker.SendMatchMakerRequest(new MatchMakerRequest()
			{
				name = "HighLevel",
			});
		}
	}

	private void OnChannelMessageReceived(string peerUUID, ushort channel, string message)
	{

	}
}

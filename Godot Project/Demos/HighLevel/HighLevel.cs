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

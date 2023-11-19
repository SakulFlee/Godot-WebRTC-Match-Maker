using Godot;

public partial class Multiplayer : Node
{
	private MatchMaker matchMaker;
	private bool requestSend = false;

	public override void _Ready()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");
	}

	public override void _Process(double delta)
	{
		if (!requestSend && matchMaker.IsReady())
		{
			var error = matchMaker.SendRequest(new MatchMakingRequest()
			{
				name = "Test",
				slots = 2,
			});
			requestSend = error == Error.Ok;
		}
	}
}

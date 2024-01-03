using Godot;

public partial class RPC : Node
{
	private MatchMaker matchMaker;

	private Label ActualCounterLabel;

	public override void _EnterTree()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");

		GetNode<DebugPanel>("%DebugPanel").matchMaker = matchMaker;
		GetNode<ConnectionPanel>("%ConnectionPanel").matchMaker = matchMaker;

		Multiplayer.MultiplayerPeer = new MatchMakerMultiplayerPeer(matchMaker);
	}

	public override void _Ready()
	{
		ActualCounterLabel = GetNode<Label>("%ActualCounterLabel");
	}

	public override void _Process(double delta)
	{
		if (matchMaker.IsReady() && !matchMaker.RequestSend)
		{
			matchMaker.SendMatchMakerRequest(new MatchMakerRequest()
			{
				name = "RPC",
			});
		}
	}

	public void OnPlusButtonPressed()
	{
		var currentCounter = int.Parse(ActualCounterLabel.Text);
		var newCounter = currentCounter + 1;
		Rpc("CounterUpdate", newCounter);
	}

	public void OnMinusButtonPressed()
	{
		var currentCounter = int.Parse(ActualCounterLabel.Text);
		var newCounter = currentCounter - 1;
		Rpc("CounterUpdate", newCounter);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void CounterUpdate(int counter)
	{
		GD.Print("CounterUpdate");

		ActualCounterLabel.Text = counter.ToString();
	}
}

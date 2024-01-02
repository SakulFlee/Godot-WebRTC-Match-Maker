using Godot;

// TODO: Implement for WebRTC peers or reuse somehow

public partial class HighLevel : Node
{
	[Export]
	public PackedScene playerScene;

	private MatchMaker matchMaker;

	private bool hostGotAdded = false;

	public override void _EnterTree()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");

		GetNode<DebugPanel>("%DebugPanel").matchMaker = matchMaker;
		GetNode<ConnectionPanel>("%ConnectionPanel").matchMaker = matchMaker;

		Multiplayer.MultiplayerPeer = new MatchMakerMultiplayerPeer(matchMaker);
	}

	public override void _Ready()
	{
		Multiplayer.PeerConnected += (id) =>
		{
			GD.Print($"[HighLevel@{matchMaker.OwnUUID}; Is Host? {matchMaker.IsHost}] Peer connected: {id}");

			if (IsMultiplayerAuthority())
			{
				if (matchMaker.IsHost && !hostGotAdded)
				{
					AddPlayer(1);

					hostGotAdded = true;
				}

				AddPlayer(id);
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

	public void AddPlayer(long id)
	{
		var player = playerScene.Instantiate<HighLevelPlayer>();
		player.Name = $"{id}";
		AddChild(player);
	}
}

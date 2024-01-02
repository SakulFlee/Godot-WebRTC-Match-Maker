using Godot;

public partial class HighLevel : Node
{
	public const int port = 33333;

	[Export]
	public PackedScene playerScene;

	private ENetMultiplayerPeer peer = new();

	private void hideUI()
	{
		GetNode<Control>("%UI").Hide();
	}

	public void AddPlayer(long id)
	{
		var player = playerScene.Instantiate<HighLevelPlayer>();
		player.Name = $"{id}";
		AddChild(player);
	}

	public void OnHostButtonPressed()
	{
		Multiplayer.PeerConnected += AddPlayer;

		peer.CreateServer(port);
		Multiplayer.MultiplayerPeer = peer;

		AddPlayer(1);

		hideUI();
	}

	public void OnClientButtonPressed()
	{
		peer.CreateClient("127.0.0.1", port);
		Multiplayer.MultiplayerPeer = peer;

		hideUI();
	}
}

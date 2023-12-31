using Godot;

public partial class ConnectionPanel : Control
{
	[Export]
	public MatchMaker matchMaker;

	private Label label;

	public override void _EnterTree()
	{
		label = GetNode<Label>("Label");

		Show();
	}

	public override void _Ready()
	{
		matchMaker.OnNewConnection += (peerUUID) =>
				{
					matchMaker.webRTCConnections[peerUUID].OnConnectionStateChange += (state) =>
					{
						if (state == "connected")
						{
							Hide();
						}
					};
				};
		matchMaker.OnMatchMakerUpdate += OnMatchMakerUpdate;
	}

	private void OnMatchMakerUpdate(uint currentPeerCount, uint requiredPeerCount)
	{
		label.Text = $"Waiting for players ...\n{currentPeerCount}/{requiredPeerCount}";
	}
}

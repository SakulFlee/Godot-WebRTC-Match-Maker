using Godot;

public partial class PingPong : Node
{
	private MatchMaker matchMaker;

	private Label sendPingLabel;
	private uint sendPingCounter = 0;

	private Label sendPongLabel;
	private uint sendPongCounter = 0;

	private Label receivedPingLabel;
	private uint receivedPingCounter = 0;

	private Label receivedPongLabel;
	private uint receivedPongCounter = 0;


	public override void _EnterTree()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");

		GetNode<DebugPanel>("%DebugPanel").matchMaker = matchMaker;
		GetNode<ConnectionPanel>("%ConnectionPanel").matchMaker = matchMaker;

		sendPingLabel = GetNode<Label>("%SendPingLabel");
		sendPongLabel = GetNode<Label>("%SendPongLabel");
		receivedPingLabel = GetNode<Label>("%ReceivedPingLabel");
		receivedPongLabel = GetNode<Label>("%ReceivedPongLabel");
	}

	public override void _Ready()
	{
		matchMaker.OnMessageString += OnChannelMessageReceived;

		// If we are a host, wait for a channel to open and send an initial message
		matchMaker.OnChannelOpen += (peerUUID, channel) =>
		{
			if (matchMaker.IsHost)
			{
				GD.Print("[PingPong] Channel opened! Sending initial message ...");
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
				name = "PingPong",
			});
		}

		sendPingLabel.Text = sendPingCounter.ToString();
		sendPongLabel.Text = sendPongCounter.ToString();
		receivedPingLabel.Text = receivedPingCounter.ToString();
		receivedPongLabel.Text = receivedPongCounter.ToString();
	}

	private void OnChannelMessageReceived(string peerUUID, ushort channel, string message)
	{
		// Send back Pings and Pongs!
		if (message == "Ping!")
		{
			matchMaker.SendOnChannelString(peerUUID, channel, "Pong!");

			receivedPingCounter++;
			sendPongCounter++;
		}
		else if (message == "Pong!")
		{
			matchMaker.SendOnChannelString(peerUUID, channel, "Ping!");

			receivedPongCounter++;
			sendPingCounter++;
		}
		else
		{
			GD.Print($"[PingPong] Invalid message '{message}' received!");
		}
	}
}

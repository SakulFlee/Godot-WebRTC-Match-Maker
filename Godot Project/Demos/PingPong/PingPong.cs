using Godot;

public partial class PingPong : Node
{
	private RichTextLabel LabelMatchMaker;
	private RichTextLabel LabelLocalState;
	private RichTextLabel LabelRemoteState;
	private RichTextLabel LabelMessages;

	private MatchMaker matchMaker;
	private bool requestSend = false;
	private bool initialMessageSend = false;

	public override void _Ready()
	{
		LabelMatchMaker = GetNode<RichTextLabel>("%LabelMatchMaker");
		LabelLocalState = GetNode<RichTextLabel>("%LabelLocalState");
		LabelRemoteState = GetNode<RichTextLabel>("%LabelRemoteState");
		LabelMessages = GetNode<RichTextLabel>("%LabelMessages");
		LabelMessages.Text = "Messages:";

		matchMaker = GetNode<MatchMaker>("MatchMaker");
		matchMaker.OnMessageString += ChannelMessageReceived;

		UpdateLabel();
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

		if (!initialMessageSend)
		{
			foreach (var (_, value) in matchMaker.webRTCConnections)
			{
				if (value.IsChannelOpen(WebRTCPeer.MAIN_CHANNEL_ID))
				{
					value.SendOnChannel(WebRTCPeer.MAIN_CHANNEL_ID, "Ping!");

					initialMessageSend = true;
				}
			}
		}

		UpdateLabel();
	}

	private void UpdateLabel()
	{
		// 		var localICECandidates = "";
		// 		foreach (var localICECandidate in matchMaker.LocalICECandidates)
		// 		{
		// 			localICECandidates += "\t" + localICECandidate.ToString() + "\n";
		// 		}
		// 		if (localICECandidates.Length > 0)
		// 		{
		// 			localICECandidates = localICECandidates.Substr(0, localICECandidates.Length - 1);
		// 		}
		// 		else
		// 		{
		// 			localICECandidates = "\tNo ICE Candidates!";
		// 		}

		// 		var remoteICECandidates = "";
		// 		foreach (var remoteICECandidate in matchMaker.RemoteICECandidates)
		// 		{
		// 			remoteICECandidates += "\t" + remoteICECandidate.ToString() + "\n";
		// 		}
		// 		if (remoteICECandidates.Length > 0)
		// 		{
		// 			remoteICECandidates = remoteICECandidates.Substr(0, remoteICECandidates.Length - 1);
		// 		}
		// 		else
		// 		{
		// 			localICECandidates = "\tNo ICE Candidates!";
		// 		}

		// 		var localSession = "";
		// 		if (matchMaker.LocalSession != (null, null))
		// 		{
		// 			localSession = $"{matchMaker.LocalSession.Item1}:\n\t{matchMaker.LocalSession.Item2.Replace("\n", "\n\t\t")}";
		// 		}

		// 		var remoteSession = "";
		// 		if (matchMaker.RemoteSession != (null, null))
		// 		{
		// 			remoteSession = $"{matchMaker.RemoteSession.Item1}:\n\t{matchMaker.RemoteSession.Item2.Replace("\n", "\n\t\t")}";
		// 		}

		// 		var peers = "";
		// 		foreach (var (uuid, _) in matchMaker.webRTCConnections)
		// 		{
		// 			peers += "\t" + uuid + "\n";
		// 		}
		// 		if (peers.Length > 0)
		// 		{
		// 			peers = peers.Substr(0, peers.Length - 1);
		// 		}

		// 		LabelMatchMaker.Text = $@"Match Maker:
		// 	Is Ready: {matchMaker.IsReady()}
		// 	Peer Status: {matchMaker.peer.GetReadyState()}
		// 	Request send: {requestSend}
		// 	Initial message send: {initialMessageSend}

		// Peers:
		// {peers}
		// ";
		// 		LabelLocalState.Text = $@"Local State:
		// {localICECandidates}
		// ------------------------------------------------------------------------------------------------------------------------
		// {localSession}
		// ";
		// 		LabelRemoteState.Text = $@"Remote State:
		// {remoteICECandidates}
		// ------------------------------------------------------------------------------------------------------------------------
		// {remoteSession}
		// ";
	}

	private void ChannelMessageReceived(string peerUUID, ushort channel, string message)
	{
		LabelMessages.Text += $"\n[{peerUUID}@{channel}]\n{message}\n";

		// Send back Pings and Pongs!
		if (message == "Ping!")
		{
			matchMaker.SendOnChannelString(peerUUID, channel, "Pong!");
		}
		else if (message == "Pong!")
		{
			matchMaker.SendOnChannelString(peerUUID, channel, "Ping!");
		}
		else
		{
			GD.PrintErr("Invalid ping/pong received!");
		}
	}
}

using Godot;

public partial class Main : Node
{
	private RichTextLabel LabelMatchMaker;
	private RichTextLabel LabelLocalState;
	private RichTextLabel LabelRemoteState;
	private RichTextLabel LabelMessages;

	private MatchMaker matchMaker;
	private bool requestSend = false;

	public override void _Ready()
	{
		LabelMatchMaker = GetNode<RichTextLabel>("%LabelMatchMaker");
		LabelLocalState = GetNode<RichTextLabel>("%LabelLocalState");
		LabelRemoteState = GetNode<RichTextLabel>("%LabelRemoteState");
		LabelMessages = GetNode<RichTextLabel>("%LabelMessages");

		matchMaker = GetNode<MatchMaker>("MatchMaker");

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

		UpdateLabel();
	}

	private void UpdateLabel()
	{
		var localICECandidates = "";
		foreach (var localICECandidate in matchMaker.LocalICECandidates)
		{
			localICECandidates += "\t" + localICECandidate.ToString() + "\n";
		}
		if (localICECandidates.Length > 0)
		{
			localICECandidates = localICECandidates.Substr(0, localICECandidates.Length - 1);
		}
		else
		{
			localICECandidates = "\tNo ICE Candidates!";
		}

		var remoteICECandidates = "";
		foreach (var remoteICECandidate in matchMaker.RemoteICECandidates)
		{
			remoteICECandidates += "\t" + remoteICECandidate.ToString() + "\n";
		}
		if (remoteICECandidates.Length > 0)
		{
			remoteICECandidates = remoteICECandidates.Substr(0, remoteICECandidates.Length - 1);
		}
		else
		{
			localICECandidates = "\tNo ICE Candidates!";
		}

		var localSession = "";
		if (matchMaker.LocalSession != (null, null))
		{
			localSession = $"{matchMaker.LocalSession.Item1}:\n{matchMaker.LocalSession.Item2.Replace("\n", "\n\t")}";
		}

		var remoteSession = "";
		if (matchMaker.RemoteSession != (null, null))
		{
			remoteSession = $"{matchMaker.RemoteSession.Item1}:\n{matchMaker.RemoteSession.Item2.Replace("\n", "\n\t")}";
		}

		var peers = "";
		foreach (var (uuid, _) in matchMaker.webRTCConnections)
		{
			peers += "\t" + uuid + "\n";
		}
		if (peers.Length > 0)
		{
			peers = peers.Substr(0, peers.Length - 1);
		}

		LabelMatchMaker.Text = $@"Match Maker:
	Is Ready: {matchMaker.IsReady()}
	Peer Status: {matchMaker.peer.GetReadyState()}
	Request send: {requestSend}

Peers:
{peers}
";
		LabelLocalState.Text = $@"Local State:
{localICECandidates}
------------------------------------------------------------------------------------------------------------------------
{localSession}
";
		LabelRemoteState.Text = $@"Remote State:
{remoteICECandidates}
------------------------------------------------------------------------------------------------------------------------
{remoteSession}
";
		LabelMessages.Text = $@"Messages:

";
	}
}

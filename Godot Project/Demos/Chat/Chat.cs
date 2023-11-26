using Godot;

public partial class Chat : Node
{
// 	private RichTextLabel LabelMatchMaker;
// 	private RichTextLabel LabelLocalState;
// 	private RichTextLabel LabelRemoteState;
// 	private RichTextLabel LabelMessages;
// 	private TextEdit InputField;

// 	private MatchMaker matchMaker;
// 	private bool requestSend = false;

// 	public override void _Ready()
// 	{
// 		LabelMatchMaker = GetNode<RichTextLabel>("%LabelMatchMaker");
// 		LabelLocalState = GetNode<RichTextLabel>("%LabelLocalState");
// 		LabelRemoteState = GetNode<RichTextLabel>("%LabelRemoteState");
// 		LabelMessages = GetNode<RichTextLabel>("%LabelMessages");
// 		LabelMessages.Text = "Messages:";
// 		InputField = GetNode<TextEdit>("%InputField");

// 		matchMaker = GetNode<MatchMaker>("MatchMaker");
// 		matchMaker.ChannelMessageReceived += ChannelMessageReceived;

// 		UpdateLabel();
// 	}

// 	public override void _Process(double delta)
// 	{
// 		if (!requestSend && matchMaker.IsReady())
// 		{
// 			var error = matchMaker.SendRequest(new MatchMakingRequest()
// 			{
// 				name = "Test",
// 			});
// 			requestSend = error == Error.Ok;
// 		}

// 		UpdateLabel();
// 	}

// 	private void UpdateLabel()
// 	{
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
// 	}

// 	private void ChannelMessageReceived(string peerUUID, string channel, byte[] data)
// 	{
// 		var message = data.GetStringFromUtf8();

// 		LabelMessages.Text += $"\n[{peerUUID}@{channel}]\n{message}\n";
// 	}

// 	private void ProcessSendMessage()
// 	{
// 		var text = InputField.Text;
// 		InputField.Clear();

// 		LabelMessages.Text += $"[Us]\n{text}\n";

// 		matchMaker.BroadcastMessageOnChannel("main", text.ToUtf8Buffer());
// 	}

// 	private void OnTextChanged()
// 	{
// 		if (InputField.Text.EndsWith("\n"))
// 		{
// 			ProcessSendMessage();
// 		}
// 	}

// 	private void OnSendButtonPressed()
// 	{
// 		ProcessSendMessage();
// 	}
}

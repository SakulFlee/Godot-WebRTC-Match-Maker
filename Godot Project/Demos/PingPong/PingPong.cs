using System;
using Godot;

public partial class PingPong : Node
{
	private RichTextLabel DebugLabel;
	private Label ConnectionLabel;

	private MatchMaker matchMaker;
	private bool requestSend = false;
	private bool initialMessageSend = false;

	private string template;

	private string signalingState = "None";
	private string connectionState = "None";
	private string iceConnectionState = "None";
	private string iceGatheringState = "None";

	private bool connected = false;
	private bool connectionLabelChanged = false;
	private uint sendPingCounter = 0;
	private uint sendPongCounter = 0;
	private uint receivedPingCounter = 0;
	private uint receivedPongCounter = 0;

	public override void _EnterTree()
	{
		DebugLabel = GetNode<RichTextLabel>("%DebugLabel");
		ConnectionLabel = GetNode<Label>("%ConnectionLabel");

		template = DebugLabel.Text;
		DebugLabel.Text = "";
	}

	public override void _Ready()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");
		matchMaker.OnMessageString += ChannelMessageReceived;
		matchMaker.OnNewConnection += (peerUUID) =>
		{
			matchMaker.webRTCConnections[peerUUID].OnSignalingStateChange += (state) =>
			{
				signalingState = state;
			};
			matchMaker.webRTCConnections[peerUUID].OnConnectionStateChange += (state) =>
			{
				connectionState = state;

				if (state == "connected")
				{
					connected = true;
				}
			};
			matchMaker.webRTCConnections[peerUUID].OnICEConnectionStateChange += (state) =>
			{
				iceConnectionState = state;
			};
			matchMaker.webRTCConnections[peerUUID].OnICEGatheringStateChange += (state) =>
			{
				iceGatheringState = state;
			};
		};

		UpdateLabel();
	}

	public override void _Process(double delta)
	{
		if (!requestSend && matchMaker.IsReady())
		{
			var error = matchMaker.SendMatchMakingRequest(new MatchMakingRequest()
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
		var peersString = "";
		foreach (var (peerUUID, _) in matchMaker.webRTCConnections)
		{
			peersString += $"- {peerUUID}";
		}

		DebugLabel.Text = string.Format(template, new[] {
			signalingState,
			connectionState,
			iceConnectionState,
			iceGatheringState,
			matchMaker.OwnUUID,
			matchMaker.HostUUID,
			matchMaker.IsHost ? "yes" : "no",
			peersString
		});

		if (connected)
		{
			ConnectionLabel.Text = $@"Send           Pings: {sendPingCounter} | Pongs: {sendPongCounter}
Received    Pings: {receivedPingCounter} | Pongs: {receivedPongCounter}";
		}


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
		// LabelMessages.Text += $"\n[{peerUUID}@{channel}]\n{message}\n";

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
			GD.PrintErr("Invalid ping/pong received!");
		}
	}
}

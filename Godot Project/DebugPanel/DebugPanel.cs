using System.Collections.Generic;
using Godot;

public partial class DebugPanel : PanelContainer
{
	[Export]
	public MatchMaker matchMaker;

	private RichTextLabel debugLabel;
	private string debugTemplateText;

	private string signalingState = "None";
	private string connectionState = "None";
	private string iceConnectionState = "None";
	private string iceGatheringState = "None";

	public override void _EnterTree()
	{
		debugLabel = GetNode<RichTextLabel>("MarginContainer/DebugLabel");
		debugTemplateText = debugLabel.Text;
	}

	public override void _Ready()
	{
		matchMaker.OnNewConnection += (peerUUID) =>
		{
			matchMaker.webRTCConnections[peerUUID].OnSignalingStateChange += (state) =>
			{
				signalingState = state;
			};
			matchMaker.webRTCConnections[peerUUID].OnConnectionStateChange += (state) =>
			{
				connectionState = state;
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
	}

	public override void _Process(double delta)
	{
		debugLabel.Text = string.Format(debugTemplateText, new[] {
			signalingState,
			connectionState,
			iceConnectionState,
			iceGatheringState,
			matchMaker.OwnUUID,
			matchMaker.HostUUID,
			matchMaker.IsHost ? "yes" : "no",
			makePeerString(),
			makeChannelString(),
		});
	}

	private string makePeerString()
	{
		var peersString = "";
		foreach (var (peerUUID, _) in matchMaker.webRTCConnections)
		{
			peersString += $"- {peerUUID}";
		}
		return peersString;
	}

	private string makeChannelString()
	{
		// Place every channel name into a Set.
		// Ensures that there are no duplicates across different channels.
		var channels = new HashSet<string>();
		foreach (var (_, connection) in matchMaker.webRTCConnections)
		{
			foreach (string channel in connection.DataChannels)
			{
				channels.Add(channel);
			}
		}

		var s = "";
		foreach (var channel in channels)
		{
			s += $"- {channel}\n";
		}
		;
		return s.Remove(s.Length);
	}
}

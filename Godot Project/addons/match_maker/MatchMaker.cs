using Godot;
using Godot.Collections;
using TinyJson;

[GlobalClass]
public partial class MatchMaker : Node
{
    #region Exports
    /// <summary>
    /// The connection string to the Match Maker server.
    /// This should be in the format of:
    /// ws://<ip or domain>:<port>
    /// 
    /// Or, for SSL secured sockets:
    /// wss://<ip or domain>:<port>
    /// </summary>
    [Export]
    public string MatchMakerConnectionString = "ws://127.0.0.1:33333";

    [Export]
    public Array ICEServers = new() {
        new Dictionary() {
            {"url", "stun.l.google.com:19302"},
            {"url", "stun1.l.google.com:19302"},
            {"url", "stun2.l.google.com:19302"},
            {"url", "stun3.l.google.com:19302"},
            {"url", "stun4.l.google.com:19302"},
        }
    };
    #endregion

    #region Fields
    private WebSocketPeer peer = new();

    public Dictionary<string, WebRTCPeer> webRTCConnections { get; private set; } = new();
    #endregion

    #region Signals
    [Signal]
    public delegate void OnMessageRawEventHandler(string peerUUID, ushort channelId, byte[] data);

    [Signal]
    public delegate void OnMessageStringEventHandler(string peerUUID, ushort channelId, string message);
    #endregion

    #region Godot 
    public override void _Ready()
    {
        var err = peer.ConnectToUrl(MatchMakerConnectionString);
        if (err != Error.Ok)
        {
            GD.PrintErr($"[MatchMaker] Failed connecting to Match Maker! ({err})");
            return;
        }
    }

    public override void _Process(double delta)
    {
        peer.Poll();
        if (peer.GetReadyState() == WebSocketPeer.State.Open)
        {
            // Read packages if available
            while (peer.GetAvailablePacketCount() > 0)
            {
                var message = peer.GetPacket().GetStringFromUtf8();
                GD.Print("Message: " + message);

                var packet = Packet.FromJSON(message);
                if (packet == null)
                {
                    GD.PrintErr("[MatchMaker] Invalid JSON received! (parsing failed)");
                    GetTree().Quit();
                    return;
                }

                switch (packet.type)
                {
                    case PacketType.MatchMakerResponse:
                        var matchMakerResponse = packet.ParseMatchMakingResponse();

                        foreach (var peerUUID in matchMakerResponse.peers)
                        {
                            var connection = new WebRTCPeer()
                            {
                                Name = $"WebRTCConnection#{peerUUID}",
                                IsHost = matchMakerResponse.isHost,
                                ICEServers = ICEServers,
                            };
                            connection.OnICECandidateJSON += (json) =>
                            {
                                CallDeferred("signalOnICECandidate", peerUUID, json);
                            };
                            connection.OnMessageRaw += (channelId, data) =>
                            {
                                CallDeferred("signalOnMessageRaw", peerUUID, channelId, data);
                            };
                            connection.OnMessageString += (channelId, message) =>
                            {
                                CallDeferred("signalOnMessageString", peerUUID, channelId, message);
                            };

                            webRTCConnections.Add(peerUUID, connection);
                        }

                        break;
                    case PacketType.SessionDescription:
                        var sessionDescription = packet.ParseSessionDescription();

                        webRTCConnections[packet.uuid].AutomatedFinish(sessionDescription);

                        break;
                    case PacketType.ICECandidate:
                        var iceCandidate = packet.ParseICECandidate();

                        webRTCConnections[packet.uuid].AddICECandidate(iceCandidate);

                        break;
                    default:
                        GD.PrintErr("[MatchMaker] Invalid or unrecognized package received from Server!");
                        return;
                }
            }
        }
    }
    #endregion

    #region WebRTC Events
    private void signalOnICECandidate(string peerUUID, string json)
    {
        var packet = new Packet()
        {
            type = PacketType.ICECandidate,
            uuid = peerUUID,
            json = json,
        };

        peer.SendText(packet.ToJSON());
    }

    private void signalOnMessageRaw(string peerUUID, ushort channelId, byte[] data)
    {
        EmitSignal(SignalName.OnMessageString, peerUUID, channelId, data);
    }

    private void signalOnMessageString(string peerUUID, ushort channelId, string message)
    {
        EmitSignal(SignalName.OnMessageString, peerUUID, channelId, message);
    }
    #endregion

    #region Methods
    public bool IsReady()
    {
        return peer.GetReadyState() == WebSocketPeer.State.Open;
    }

    public Error SendRequest(MatchMakingRequest matchMaking)
    {
        if (peer.GetReadyState() != WebSocketPeer.State.Open)
        {
            return Error.Failed;
        }

        var packet = new Packet()
        {
            type = PacketType.MatchMakerRequest,
            json = matchMaking.ToJson(),
        };

        return peer.SendText(packet.ToJson());
    }

    public void SendOnChannelRaw(string peerUUID, ushort channelId, byte[] data)
    {
        webRTCConnections[peerUUID].SendOnChannelRaw(channelId, data);
    }

    public void SendOnChannelString(string peerUUID, ushort channelId, string message)
    {
        webRTCConnections[peerUUID].SendOnChannel(channelId, message);
    }

    public bool IsChannelOpen(string peerUUID, ushort channelId)
    {
        return webRTCConnections[peerUUID].IsChannelOpen(channelId);
    }
    #endregion
}

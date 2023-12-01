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

    /// <summary>
    /// Will be copied to any <see cref="WebRTCPeer"/>.
    /// Check <see cref="WebRTCPeer.ICEServers"/> for more.
    /// </summary>
    [Export]
    public Array ICEServers = new() {
        new Dictionary() {
            {"url", "stun.l.google.com:19302"},
        },
        new Dictionary() {
            {"url", "stun1.l.google.com:19302"},
        },
        new Dictionary() {
            {"url", "stun2.l.google.com:19302"},
        },
        new Dictionary() {
            {"url", "stun3.l.google.com:19302"},
        },
        new Dictionary() {
            {"url", "stun4.l.google.com:19302"},
        }
    };

    /// <summary>
    /// ICE Candidates that are allowed and will be parsed through to the <see cref="WebRTCPeer"/>.
    /// 
    /// Check <see cref="CandidateFilter"/> for more.
    /// </summary>
    [Export]
    public CandidateFilter AllowedCandidateTypes = CandidateFilter.All;

    #endregion

    #region Fields
    /// <summary>
    /// <see cref="WebSocketPeer"/> to the Match Maker Server
    /// </summary>
    private WebSocketPeer peer = new();

    /// <summary>
    /// Collection of each <see cref="WebRTCPeer"/>.
    /// 
    /// The key is the PeerUUID.
    /// </summary>
    public Dictionary<string, WebRTCPeer> webRTCConnections { get; private set; } = new();

    /// <summary>
    /// This Peers own UUID
    /// </summary>
    public string OwnUUID { get; private set; }

    /// <summary>
    /// The Host UUID
    /// </summary>
    public string HostUUID { get; private set; }

    /// <summary>
    /// Whether this peer is a host or not.
    /// This is done by comparing the <see cref="OwnUUID"/> against <see cref="HostUUID"/>.
    /// </summary>
    public bool IsHost
    {
        get { return OwnUUID != null && HostUUID != null && HostUUID == OwnUUID; }
    }

    #endregion

    #region Signals
    /// <summary>
    /// Emitted once there is a new connection being made (from <see cref="WebRTCPeer"/>)
    /// </summary>
    /// <param name="peerUUID">The peer UUID which established the connection.</param>
    [Signal]
    public delegate void OnNewConnectionEventHandler(string peerUUID);

    /// <summary>
    /// Emitted once a new message is received.
    /// This is the RAW (= byte[]) version, <seealso cref="OnMessageStringEventHandler"/> for a string (UTF8 based) version.
    /// </summary>
    /// <param name="peerUUID">The peer UUID where this message came from</param>
    /// <param name="channelId">The channel ID this message was on</param>
    /// <param name="data">The RAW data array</param>
    [Signal]
    public delegate void OnMessageRawEventHandler(string peerUUID, ushort channelId, byte[] data);

    /// <summary>
    /// Emitted once a new message is received.
    /// This is the String version, <seealso cref="OnMessageRawEventHandler"/> for a RAW (= byte[]) version.
    /// </summary>
    /// <param name="peerUUID">The peer UUID where this message came from</param>
    /// <param name="channelId">The channel ID this message was on</param>
    /// <param name="message">The string that got send</param>
    [Signal]
    public delegate void OnMessageStringEventHandler(string peerUUID, ushort channelId, string message);

    /// <summary>
    /// Gets called when a channel (including the main channel [<see cref="WebRTCPeer.MAIN_CHANNEL_ID"/>]) opens and is ready to send/retrieve messages.
    /// This is an alternative to:
    /// <seealso cref="OnChannelClose"/>
    /// <seealso cref="OnChannelStateChange"/>
    /// </summary>
    /// <param name="peerUUID">Peer this channel was opened</param>
    /// <param name="channel">Channel that was opened</param>
    [Signal]
    public delegate void OnChannelOpenEventHandler(string peerUUID, ushort channel);

    /// <summary>
    /// Gets called when a channel (including the main channel [<see cref="WebRTCPeer.MAIN_CHANNEL_ID"/>]) closes and is no longer able to send/retrieve messages.
    /// This is an alternative to:
    /// <seealso cref="OnChannelOpen"/>
    /// <seealso cref="OnChannelStateChange"/>
    /// </summary>
    /// <param name="peerUUID">Peer this channel was opened</param>
    /// <param name="channel">Channel that was opened</param>
    [Signal]
    public delegate void OnChannelCloseEventHandler(string peerUUID, ushort channel);

    /// <summary>
    /// Gets called when a channel (including the main channel [<see cref="WebRTCPeer.MAIN_CHANNEL_ID"/>]) either opens or closes.
    /// This is an alternative to:
    /// <seealso cref="OnChannelOpen"/>
    /// <seealso cref="OnChannelClose"/>
    /// </summary>
    /// <param name="peerUUID">Peer this channel was opened</param>
    /// <param name="channel">Channel that was opened</param>
    /// <param name="isOpen">Whether the channel opened or closed</param>
    [Signal]
    public delegate void OnChannelStateChangeEventHandler(string peerUUID, ushort channel, bool isOpen);
    #endregion

    #region Godot 
    public override void _Ready()
    {
        // Connect to Match Maker Server
        var err = peer.ConnectToUrl(MatchMakerConnectionString);
        if (err != Error.Ok)
        {
            GD.PrintErr($"[MatchMaker] Failed connecting to Match Maker! ({err})");
            return;
        }
    }

    public override async void _Process(double delta)
    {
        // Poll WebSocket (Match Maker Server connection), 
        // process if socket is connected
        peer.Poll();
        if (peer.GetReadyState() == WebSocketPeer.State.Open)
        {
            // Read packages if available
            while (peer.GetAvailablePacketCount() > 0)
            {
                var message = peer.GetPacket().GetStringFromUtf8();

#if DEBUG
                GD.Print("Message: " + message);
#endif

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
                        OwnUUID = packet.to;
                        GD.Print($"[MatchMaker] Own UUID: {OwnUUID}");

                        HostUUID = matchMakerResponse.hostUUID;
                        GD.Print($"[MatchMaker] Host UUID: {OwnUUID}");

                        GD.Print($"[MatchMaker] Is Host: {IsHost}");

                        foreach (var peerUUID in matchMakerResponse.peers)
                        {
                            if (peerUUID == OwnUUID)
                            {
                                // Skip if it's our own peer UUID
                                continue;
                            }

                            // Create connection
                            var connection = new WebRTCPeer()
                            {
                                Name = $"WebRTCConnection#{peerUUID}",
                                IsHost = IsHost,
                                ICEServers = ICEServers,
                            };

                            // Add Signal listeners
                            // Small hack: Calling another function deferred which then emits the Signal fixes an async issue with how WebRTCPeer handles events
                            connection.OnICECandidateJSON += (json) =>
                            {
                                CallDeferred("signalOnICECandidate", peerUUID, json);
                            };
                            connection.OnMessageRaw += (channelId, data) =>
                            {
                                CallDeferred("signalOnMessageRaw", peerUUID, channelId, data);
                            };
                            connection.OnMessageRaw += (channelId, data) =>
                            {
                                CallDeferred("signalOnMessageRaw", peerUUID, channelId, data);
                            };
                            connection.OnMessageString += (channelId, message) =>
                            {
                                CallDeferred("signalOnMessageString", peerUUID, channelId, message);
                            };
                            connection.OnChannelOpen += (channel) =>
                            {
                                CallDeferred("signalOnChannelOpen", peerUUID, channel);
                            };
                            connection.OnChannelClose += (channel) =>
                            {
                                CallDeferred("signalOnChannelClose", peerUUID, channel);
                            };
                            connection.OnChannelStateChange += (channel, isOpen) =>
                            {
                                CallDeferred("signalOnChannelStateChange", peerUUID, channel, isOpen);
                            };

                            webRTCConnections.Add(peerUUID, connection);
                            AddChild(connection);

                            // Hosts are expected to start the connection process by creating an 'offer' and sending that to the client peer.
                            // If we are a host, do that.
                            // This also sets the 'local' session description.
                            if (IsHost)
                            {
                                var session = connection.CreateOffer();
                                await connection.SetLocalDescription(session);

                                var json = session.toJSON();
                                SendPacket(PacketType.SessionDescription, peerUUID, json);
                            }

                            // Signal new connection opened
                            EmitSignal(SignalName.OnNewConnection, peerUUID);
                        }

                        break;
                    case PacketType.SessionDescription:
                        var sessionDescription = packet.ParseSessionDescription();

                        // Set the session description we received.
                        // This always will be the 'remote' session description.
                        var sessionConnection = webRTCConnections[packet.from];
                        sessionConnection.SetRemoteDescription(sessionDescription);

                        // Clients are expected to create an 'answer' once an 'offer' is received and set.
                        // If we are a client, do that.
                        // This also sets the 'local' session description.
                        if (!IsHost)
                        {
                            var session = sessionConnection.CreateAnswer();
                            await sessionConnection.SetLocalDescription(session);

                            var json = session.toJSON();
                            SendPacket(PacketType.SessionDescription, packet.from, json);
                        }

                        break;
                    case PacketType.ICECandidate:
                        var iceCandidate = packet.ParseICECandidate();

                        // Filter the ICE Candidate we received based on the CandidateFilter
                        if (
                            // All are allowed
                            AllowedCandidateTypes == CandidateFilter.All
                        || (
                            // Or: Relay is allowed
                            AllowedCandidateTypes == CandidateFilter.Relay
                            && iceCandidate.candidate.Contains("relay")
                            )
                        || (
                            // Or: Host is allowed
                            AllowedCandidateTypes == CandidateFilter.Host
                            && iceCandidate.candidate.Contains("host")
                            )
                        || (
                            // Or: Server Reflexiv is allowed
                            AllowedCandidateTypes == CandidateFilter.ServerReflexiv
                            && iceCandidate.candidate.Contains("srflx")
                            )
                        || (
                            // Or: Peer Reflexiv is allowed
                            AllowedCandidateTypes == CandidateFilter.PeerReflexiv
                            && iceCandidate.candidate.Contains("prflx")
                            )
                        )
                        {
                            // If it passed the filter: Add it!
                            webRTCConnections[packet.from].AddICECandidate(iceCandidate);
                        }

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
    /// <summary>
    /// Sends a ICE candidate (in JSON form) to the correct peer 
    /// via the Match Maker server.
    /// </summary>
    /// <param name="peerUUID">Peer UUID to send this to</param>
    /// <param name="json">ICE Candidate in JSON form</param>
    private void signalOnICECandidate(string peerUUID, string json)
    {
        var packet = new Packet()
        {
            type = PacketType.ICECandidate,
            from = OwnUUID,
            to = peerUUID,
            json = json,
        };

        peer.SendText(packet.ToJSON());
    }

    /// <summary>
    /// ⚠️ Workaround: Must be called deferred.
    /// 
    /// Will emit the <see cref="OnMessageRawEventHandler"/> signal.
    /// 
    /// <seealso cref="signalOnMessageString(string, ushort, string)"/>
    /// </summary>
    /// <param name="peerUUID">Peer UUID from where this originated</param>
    /// <param name="channelId">Channel ID from where this message came</param>
    /// <param name="data">The data received in byte[] form</param>
    private void signalOnMessageRaw(string peerUUID, ushort channelId, byte[] data)
    {
        EmitSignal(SignalName.OnMessageRaw, peerUUID, channelId, data);
    }

    /// <summary>
    /// ⚠️ Workaround: Must be called deferred.
    /// 
    /// Will emit the <see cref="OnMessageStringEventHandler"/> signal.
    /// 
    /// <seealso cref="signalOnMessageRaw(string, ushort, byte[])"/>
    /// </summary>
    /// <param name="peerUUID">Peer UUID from where this originated</param>
    /// <param name="channelId">Channel ID from where this message came</param>
    /// <param name="message">The message received in string form</param>
    private void signalOnMessageString(string peerUUID, ushort channelId, string message)
    {
        EmitSignal(SignalName.OnMessageString, peerUUID, channelId, message);
    }

    private void signalOnChannelOpen(string peerUUID, short channel)
    {
        EmitSignal(SignalName.OnChannelOpen, peerUUID, channel);
    }

    private void signalOnChannelClose(string peerUUID, short channel)
    {
        EmitSignal(SignalName.OnChannelClose, peerUUID, channel);
    }

    private void signalOnChannelStateChange(string peerUUID, short channel, bool isOpen)
    {
        EmitSignal(SignalName.OnChannelStateChange, peerUUID, channel, isOpen);
    }
    #endregion

    #region Methods
    /// <summary>
    /// Checks if there is a connection to the Match Maker Server.
    /// </summary>
    /// <returns>true, if a connection exists, false otherwise</returns>
    public bool IsReady()
    {
        return peer.GetReadyState() == WebSocketPeer.State.Open;
    }

    /// <summary>
    /// Used to send a <see cref="MatchMakingRequest"/> to the Match Maker Server.
    /// 
    /// Make sure to check if the connection is ready first via <see cref="IsReady"/>.
    /// </summary>
    /// <param name="matchMaking">The request to be made</param>
    /// <returns>An <see cref="Error"/> if the connection is not open or an error ocurred.</returns>
    public Error SendMatchMakingRequest(MatchMakingRequest matchMaking)
    {
        if (peer.GetReadyState() != WebSocketPeer.State.Open)
        {
            return Error.Failed;
        }

        var json = matchMaking.ToJson();
        return SendPacket(PacketType.MatchMakerRequest, "MatchMaker", "UNKNOWN", json);
    }

    /// <summary>
    /// Used to send a packet to a peer in relay mode.
    /// 
    /// Make sure to check if the connection is ready first via <see cref="IsReady"/>.
    /// </summary>
    /// <param name="type">The <see cref="PacketType"/> of this packet</param>
    /// <param name="to">Where this packet is supposed to go (Peer UUID)</param>
    /// <param name="from">Where this packet is coming from (Peer UUID), commonly <see cref="OwnUUID"/></param>
    /// <param name="json">The packet itself as JSON</param>
    /// <returns>An <see cref="Error"/> if the connection is not open or an error ocurred.</returns>
    public Error SendPacket(PacketType type, string to, string from, string json)
    {
        if (peer.GetReadyState() != WebSocketPeer.State.Open)
        {
            return Error.Failed;
        }

        var packet = new Packet()
        {
            type = type,
            to = to,
            from = from,
            json = json,
        };

        return peer.SendText(packet.ToJson());
    }

    /// <summary>
    /// Simplified form of <see cref="SendPacket(PacketType, string, string, string)"/>.
    /// Assumes 'from' is <see cref="OwnUUID"/>.
    /// </summary>
    /// <param name="type">The <see cref="PacketType"/> of this packet</param>
    /// <param name="to">Where this packet is supposed to go (Peer UUID)</param>
    /// <param name="json">The packet itself as JSON</param>
    /// <returns>An <see cref="Error"/> if the connection is not open or an error ocurred.</returns>
    public Error SendPacket(PacketType type, string to, string json)
    {
        return SendPacket(type, to, OwnUUID, json);
    }

    /// <summary>
    /// Helper/Wrapper to send a message on a channel of a peer with RAW (= byte[]) data.
    /// </summary>
    /// <param name="peerUUID">The peer UUID to send this to</param>
    /// <param name="channelId">The channel to send this onn</param>
    /// <param name="data">The RAW (= byte[]) data to send</param>
    public void SendOnChannelRaw(string peerUUID, ushort channelId, byte[] data)
    {
        webRTCConnections[peerUUID].SendOnChannelRaw(channelId, data);
    }

    /// <summary>
    /// Helper/Wrapper to send a message on a channel of a peer with string data.
    /// </summary>
    /// <param name="peerUUID">The peer UUID to send this to</param>
    /// <param name="channelId">The channel to send this onn</param>
    /// <param name="message">The string data to send</param>
    public void SendOnChannelString(string peerUUID, ushort channelId, string message)
    {
        webRTCConnections[peerUUID].SendOnChannel(channelId, message);
    }

    /// <summary>
    /// Checks if a given channel on a peer is open or not.
    /// </summary>
    /// <param name="peerUUID">The peer of the channel</param>
    /// <param name="channelId">The channel to check</param>
    /// <returns>true, if the channel and peer exist and are open, false otherwise.</returns>
    public bool IsChannelOpen(string peerUUID, ushort channelId)
    {
        return webRTCConnections[peerUUID].IsChannelOpen(channelId);
    }
    #endregion
}

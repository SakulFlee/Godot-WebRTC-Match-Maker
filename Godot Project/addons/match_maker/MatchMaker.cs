using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using SIPSorcery.Net;
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
    public Array ICEServers = [
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
    ];

    /// <summary>
    /// IF SET: 
    /// Will automatically wait for a connection to the match maker server to 
    /// be stable, then send a slot request for the set slot name.
    /// 
    /// IF NOT SET:
    /// Won't automatically send a slot request.
    /// This will have to be done manually instead, like so:
    /// if (matchMaker.IsReady() && !matchMaker.RequestSend)
    /// {
    /// 	matchMaker.SendMatchMakerRequest(new MatchMakerRequest()
    ///     {
    ///         name = "SLOT NAME HERE",
    /// 	});
    /// }
    /// 
    /// Note: Not setting this value might be useful if you extend the match
    /// maker server to take in more arguments then just the slot name!
    /// </summary>
    [Export]
    public string AutoSendSlotRequest;

    /// <summary>
    /// ICE Candidates that are allowed and will be parsed through to the <see cref="WebRTCPeer"/>.
    /// 
    /// Check <see cref="CandidateFilter"/> for more.
    /// </summary>
    [Export]
    public CandidateFilter AllowedCandidateTypes = CandidateFilter.All;

    [Export]
    public Array DataChannels = ["Main"];

    [Export]
    public uint Timeout = 30 * 1000;

    [Export]
    public bool PrintIncomingMessagesToConsole = true;
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
    public Dictionary<string, WebRTCPeer> webRTCConnections { get; private set; } = [];

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

    /// <summary>
    /// Indicates if a request to the Match Maker server was send or not
    /// </summary>
    public bool RequestSend { get; private set; } = false;

    private Timer timeoutTimer;
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

    [Signal]
    public delegate void OnMatchMakerUpdateEventHandler(uint currentPeerCount, uint requiredPeerCount);

    [Signal]
    public delegate void OnMatchMakerTimeoutEventHandler(string peerUUID);
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
        // No need to poll if the peer is nulled!
        if (peer == null)
        {
            return;
        }

        // Poll the websocket connection and process packets if available
        peer.Poll();
        if (peer.GetReadyState() == WebSocketPeer.State.Open)
        {
            // Read packages if available
            while (peer.GetAvailablePacketCount() > 0)
            {
                var message = peer.GetPacket().GetStringFromUtf8();

                var packet = Packet.FromJSON<Packet>(message);
                if (packet == null)
                {
                    GD.PrintErr("[MatchMaker] Invalid JSON received! (parsing failed)");
                    return;
                }

                switch (packet.type)
                {
                    case PacketType.MatchMakerUpdate:
                        HandleMatchMakerUpdate(packet);
                        break;
                    case PacketType.MatchMakerResponse:
                        await HandleMatchMakerResponse(packet);
                        break;
                    case PacketType.SessionDescription:
                        await HandleSessionDescription(packet);
                        break;
                    case PacketType.ICECandidate:
                        HandleICECandidate(packet);
                        break;
                    default:
                        GD.PrintErr("[MatchMaker] Invalid or unrecognized package received from Server!");
                        return;
                }
            }
        }
        else if (peer.GetReadyState() == WebSocketPeer.State.Closed)
        {
            // Poll until we are closed, then null the peer
            peer = null;
        }

        // Handle automatic slot request
        if (AutoSendSlotRequest != null && !RequestSend && IsReady())
        {
            SendMatchMakerRequest(new MatchMakerRequest()
            {
                name = AutoSendSlotRequest,
            });
        }
    }

    private void HandleMatchMakerUpdate(Packet packet)
    {
        var matchMakerUpdate = packet.ParseMatchMakerUpdate();

        EmitSignal(SignalName.OnMatchMakerUpdate, matchMakerUpdate.currentPeerCount, matchMakerUpdate.requiredPeerCount);
    }

    private async Task HandleMatchMakerResponse(Packet packet)
    {
        var matchMakerResponse = packet.ParseMatchMakerResponse();
        OwnUUID = packet.to;
        GD.Print($"[MatchMaker] Own UUID: {OwnUUID}");

        HostUUID = matchMakerResponse.hostUUID;
        GD.Print($"[MatchMaker] Host UUID: {OwnUUID}");
        GD.Print($"[MatchMaker] Is Host: {IsHost}");

        timeoutTimer = new Timer()
        {
            Autostart = true,
            OneShot = true,
            WaitTime = Timeout,
        };
        timeoutTimer.Timeout += () =>
        {
            GD.PrintErr("[MatchMaker] Timeout reached!");
            EmitSignal(SignalName.OnMatchMakerTimeout, OwnUUID);
        };
        AddChild(timeoutTimer);

        if (IsHost)
        {
            // Hosts connect to every client

            foreach (var peerUUID in matchMakerResponse.peers)
            {
                if (peerUUID == OwnUUID)
                {
                    // Skip if it's our own peer UUID
                    continue;
                }

                var connection = makeWebRTCPeer(peerUUID);

                // Hosts are expected to start the connection process by creating an 'offer' and sending that to the client peer.
                // If we are a host, do that.
                // This also sets the 'local' session description.
                var session = connection.CreateOffer();
                await connection.SetLocalDescription(session);

                var json = session.toJSON();
                SendPacket(PacketType.SessionDescription, peerUUID, json);

                // Timeout
                connection.OnChannelOpen += (_) =>
                {
                    if (timeoutTimer != null)
                    {
                        RemoveChild(timeoutTimer);
                        timeoutTimer = null;
                    }
                };
            }
        }
        else
        {
            // Clients only connect to the host
            var connection = makeWebRTCPeer(matchMakerResponse.hostUUID);

            // Timeout
            connection.OnChannelOpen += (_) =>
                {
                    if (timeoutTimer != null)
                    {
                        RemoveChild(timeoutTimer);
                        timeoutTimer = null;
                    }
                };
        }
    }

    private async Task HandleSessionDescription(Packet packet)
    {
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
    }

    private void HandleICECandidate(Packet packet)
    {
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
    }

    private WebRTCPeer makeWebRTCPeer(string peerUUID)
    {
        // Create connection and add it to our local collection and scene
        var connection = new WebRTCPeer()
        {
            Name = $"WebRTCConnection#{peerUUID}",
            PrintIncomingMessagesToConsole = PrintIncomingMessagesToConsole,
            IsHost = IsHost,
            ICEServers = ICEServers,
            DataChannels = DataChannels,
        };
        webRTCConnections.Add(peerUUID, connection);
        AddChild(connection);

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

        // Emit signal about a new peer connection being made
        EmitSignal(SignalName.OnNewConnection, peerUUID);

        return connection;
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
        if (peer == null)
        {
            GD.Print($"[Match Maker] Got more ICE candidates, but peer connection seems to be already closed!\nSkipping: {json}");
            return;
        }

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

        // Once a P2P/WebRTC connection is established AND a channel is opened
        // (open channel implies a connection is made), we can close the
        // connection to the match making server.
        // NOTE: This possibly has to be changed for 2+ peers!
        if (peer != null)
        {
            var closeConnection = true;
            foreach (var (_, peer) in webRTCConnections)
            {
                if (!peer.IsReady)
                {
                    closeConnection = false;
                }
            }

            if (closeConnection)
            {
                peer.Close();
            }
        }
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
        return peer != null && peer.GetReadyState() == WebSocketPeer.State.Open;
    }

    /// <summary>
    /// Used to send a <see cref="MatchMakerRequest"/> to the Match Maker Server.
    /// 
    /// Make sure to check if the connection is ready first via <see cref="IsReady"/>.
    /// </summary>
    /// <param name="MatchMaker">The request to be made</param>
    /// <returns>An <see cref="Error"/> if the connection is not open or an error ocurred.</returns>
    public Error SendMatchMakerRequest(MatchMakerRequest MatchMaker)
    {
        if (peer.GetReadyState() != WebSocketPeer.State.Open)
        {
            return Error.Failed;
        }

        if (RequestSend)
        {
            return Error.AlreadyExists;
        }

        var json = MatchMaker.ToJson();
        var error = SendPacket(PacketType.MatchMakerRequest, "MatchMaker", "UNKNOWN", json);

        if (error == Error.Ok)
        {
            RequestSend = true;
        }

        return error;
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

    public void RemovePeer(string peerUUID, string reason)
    {
        var peer = webRTCConnections[peerUUID];
        peer.peer.Close(reason);

        webRTCConnections.Remove(peerUUID);
    }
    #endregion
}

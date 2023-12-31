using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using SIPSorcery.Net;

[GlobalClass]
public partial class WebRTCPeer : Node
{
    #region Globals
    /// <summary>
    /// The main (default) channel ID.
    /// </summary>
    public static readonly ushort MAIN_CHANNEL_ID = 0;
    #endregion

    #region Exports
    /// <summary>
    /// List of ICE Servers.
    /// This can be STUN or TURN servers.
    /// 
    /// Multiple servers can be added and mixed.
    /// 
    /// Note, that if you are required to use a username+password (credentials)
    /// you will need to add them here too.
    /// 
    /// The following format is expected:
    /// <code>
    /// {
    ///     // Without credentials:
    ///     {
    ///         "url": "stun:some-stun-server.tld:3478,
    ///     },
    ///     // With credentials:
    ///     {
    ///         "url": "stun:some-stun-server.tld:3478,
    ///         "username": "my-user",
    ///         "credential": "my-super-secure-password"
    ///     }
    /// }
    /// </code>
    /// 
    /// Or, in C# Terms:
    /// <code>
    /// var ICEServers = new Array() {
    ///     new Dictionary() {
    ///     {
    ///         { "url": "stun:some-stun-server.tld:3478 }
    ///     },
    ///     // With credentials:
    ///     new Dictionary() {
    ///         { "url": "stun:some-stun-server.tld:3478 },
    ///         { "username": "my-user" },
    ///         { "credential": "my-super-secure-password }
    ///     }
    /// }
    /// </code>
    /// 
    /// The supplied default servers are STUN servers (i.e. NO RELAY/TURN),
    /// which are freely available and hosted by Google.
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
    /// Whether the current peer is a host or not
    /// </summary>
    [Export]
    public bool IsHost = false;

    [Export]
    public Array DataChannels = ["Main"];

    [Export]
    public bool PrintIncomingMessagesToConsole = true;
    #endregion

    #region Fields
    /// <summary>
    /// The actual peer connection
    /// </summary>
    public RTCPeerConnection peer { get; private set; }

    public System.Collections.Generic.Dictionary<ushort, RTCDataChannel> channels { get; private set; } = [];

    public Array openChannels { get; private set; } = new();

    public bool IsReady
    {
        get
        {
            return openChannels.Count > 0;
        }
    }
    #endregion

    #region Signals
    /// <summary>
    /// Gets emitted on a new ICE Candidate being ready to be send to a peer.
    /// This comes already pre-serialized into JSON for easier use.
    /// If you really need to know what the details on the ICE candidate are,
    /// deserialize it in your listener.
    /// </summary>
    [Signal]
    public delegate void OnICECandidateJSONEventHandler(string json);

    /// <summary>
    /// Gets emitted on the ICE Connection State changing
    /// </summary>
    [Signal]
    public delegate void OnICEConnectionStateChangeEventHandler(string state);

    /// <summary>
    /// Gets emitted on the ICE Gathering State changing
    /// </summary>
    [Signal]
    public delegate void OnICEGatheringStateChangeEventHandler(string state);

    /// <summary>
    /// Gets emitted on the Signaling State changing
    /// </summary>
    [Signal]
    public delegate void OnSignalingStateChangeEventHandler(string state);

    /// <summary>
    /// Gets emitted on the Connection State changing
    /// </summary>
    [Signal]
    public delegate void OnConnectionStateChangeEventHandler(string state);

    /// <summary>
    /// Gets emitted on the Gathering State changing
    /// </summary>
    [Signal]
    public delegate void OnGatheringStateChangeEventHandler(string state);

    /// <summary>
    /// Gets emitted on a new message being received in RAW (=byte[]) data.
    /// <seealso cref="OnMessageStringEventHandler"/>
    /// </summary>
    [Signal]
    public delegate void OnMessageRawEventHandler(ushort channelId, byte[] data);

    /// <summary>
    /// Gets emitted on a new message being received in string data.
    /// <seealso cref="OnMessageRawEventHandler"/>
    /// </summary>
    [Signal]
    public delegate void OnMessageStringEventHandler(ushort channelId, string message);

    /// <summary>
    /// Gets called when a channel (including the main channel [<see cref="MAIN_CHANNEL_ID"/>]) opens and is ready to send/retrieve messages.
    /// This is an alternative to:
    /// <seealso cref="OnChannelClose"/>
    /// <seealso cref="OnChannelStateChange"/>
    /// </summary>
    /// <param name="channelId">The channel that opened</param>
    [Signal]
    public delegate void OnChannelOpenEventHandler(ushort channelId);

    /// <summary>
    /// Gets called when a channel (including the main channel [<see cref="MAIN_CHANNEL_ID"/>]) closed and is no longer able to send/retrieve messages.
    /// This is an alternative to:
    /// <seealso cref="OnChannelOpen"/>
    /// <seealso cref="OnChannelStateChange"/>
    /// </summary>
    /// <param name="channelId">The channel that closed</param>
    [Signal]
    public delegate void OnChannelCloseEventHandler(ushort channelId);

    /// <summary>
    /// Gets called when a channels state changes (including the main channel [<see cref="MAIN_CHANNEL_ID"/>]) 
    /// This is an alternative to:
    /// <seealso cref="OnChannelOpen"/>
    /// <seealso cref="OnChannelClose"/>
    /// </summary>
    /// <param name="channelId">The channel that opened</param>
    [Signal]
    public delegate void OnChannelStateChangeEventHandler(ushort channelId, bool open);
    #endregion

    #region Godot Functions
    public override async void _Ready()
    {
        var config = parseConfiguration();
        peer = makePeer(config);

        await Initialize();
    }

    public async Task Initialize()
    {
        await createDataChannels();
    }

    private RTCConfiguration parseConfiguration()
    {
        // Parse ICE Server config
        var rtcIceServers = new List<RTCIceServer>();
        foreach (Dictionary iceServer in ICEServers)
        {
            var url = (string)iceServer["url"];
            var rtcIceServer = new RTCIceServer
            {
                urls = url,
            };

            Variant usernameVariant;
            Variant credentialVariant;
            if (iceServer.TryGetValue("username", out usernameVariant) && iceServer.TryGetValue("credential", out credentialVariant))
            {
                string username = usernameVariant.AsString();
                string credential = credentialVariant.AsString();

                rtcIceServer.credentialType = RTCIceCredentialType.password;
                rtcIceServer.username = username;
                rtcIceServer.credential = credential;

#if DEBUG
                GD.Print($"[WebRTC] ICE Server: {url} (WITH credentials)");
            }
            else
            {
                GD.Print($"[WebRTC] ICE Server: {url} (NO credentials)");
#endif
            }

            rtcIceServers.Add(rtcIceServer);
        }

        return new RTCConfiguration()
        {
            iceServers = rtcIceServers,
        };
    }

    private RTCPeerConnection makePeer(RTCConfiguration config)
    {
        // Create a new peer with the config
        var p = new RTCPeerConnection(config);

        // Signals
        p.onicecandidate += (candidate) =>
        {
            CallDeferred("signalOnICECandidate", candidate.toJSON());
        };
        p.oniceconnectionstatechange += (state) =>
        {
            CallDeferred("signalOnICEConnectionStateChangeEventHandler", state.ToString());
        };
        p.onsignalingstatechange += () =>
        {
            CallDeferred("signalOnSignalingStateChangeEventHandler", p.signalingState.ToString());
        };
        p.onconnectionstatechange += (state) =>
        {
            CallDeferred("signalOnConnectionStateChangeEventHandler", state.ToString());
        };
        p.onicegatheringstatechange += (state) =>
        {
            CallDeferred("signalOnGatheringStateChangeEventHandler", state.ToString());
        };

        GD.Print($"[WebRTC] Peer created!");
        return p;
    }

    private async Task createDataChannels()
    {
        var channelCreations = new List<Task<ushort>>();
        var counter = 0;
        foreach (string channelName in DataChannels)
        {
            var channelCreationFuture = createChannel(channelName, (ushort)counter);
            channelCreations.Add(channelCreationFuture);

            counter++;
        }
        foreach (var future in channelCreations)
        {
            var channelId = await future;
            GD.Print($"[WebRTC] Channel #{channelId}/'{DataChannels[channelId]}' got created!");
        }
    }
    #endregion

    #region Signal Methods
    /// <summary>
    /// ⚠️ Workaround: Must be called deferred.
    /// 
    /// Will emit the <see cref="OnICECandidateJSON"/> signal.
    /// <param name="json">ICE Candidate as JSON</param>
    private void signalOnICECandidate(string json)
    {
#if DEBUG
        GD.Print($"[WebRTC] ICE Candidate: {json}");
#endif

        EmitSignal(SignalName.OnICECandidateJSON, json);
    }

    /// <summary>
    /// ⚠️ Workaround: Must be called deferred.
    /// 
    /// Will emit the <see cref="OnICEConnectionStateChange"/> signal.
    /// <param name="state">The new state</param>
    private void signalOnICEConnectionStateChangeEventHandler(string state)
    {
#if DEBUG
        GD.Print($"[WebRTC] ICE Connection State Changed: {state}");
#endif

        EmitSignal(SignalName.OnICEConnectionStateChange, state);
    }

    /// <summary>
    /// ⚠️ Workaround: Must be called deferred.
    /// 
    /// Will emit the <see cref="OnSignalingStateChange"/> signal.
    /// <param name="state">The new state</param>
    private void signalOnSignalingStateChangeEventHandler(string state)
    {
#if DEBUG
        GD.Print($"[WebRTC] Signaling State Changed: {state}");
#endif

        EmitSignal(SignalName.OnSignalingStateChange, state);
    }

    /// <summary>
    /// ⚠️ Workaround: Must be called deferred.
    /// 
    /// Will emit the <see cref="OnConnectionStateChange"/> signal.
    /// <param name="state">The new state</param>
    private void signalOnConnectionStateChangeEventHandler(string state)
    {
#if DEBUG
        GD.Print($"[WebRTC] Connection State Changed: {state}");
#endif

        EmitSignal(SignalName.OnConnectionStateChange, state);
    }

    /// <summary>
    /// ⚠️ Workaround: Must be called deferred.
    /// 
    /// Will emit the <see cref="OnGatheringStateChange"/> signal.
    /// <param name="state">The new state</param>
    private void signalOnGatheringStateChangeEventHandler(string state)
    {
#if DEBUG
        GD.Print($"[WebRTC] Gathering State Changed: {state}");
#endif

        EmitSignal(SignalName.OnICEGatheringStateChange, state);
    }

    /// <summary>
    /// ⚠️ Workaround: Must be called deferred.
    /// 
    /// Will emit the following Signals: 
    ///  - <see cref="OnMessageRaw"/>
    ///  - <see cref="OnMessageString"/>
    /// <param name="channelId">The channel this got send</param>
    /// <param name="protocol">The protocol being used</param>
    /// <param name="data">The RAW (= byte[]) data send</param>
    private void signalOnMessage(ushort channelId, string protocol, byte[] data)
    {
        var message = data.GetStringFromUtf8();

        if (PrintIncomingMessagesToConsole)
        {

            var channelLabel = GetChannelLabel(channelId);
            GD.Print($"[WebRTC] Message on #{channelId}@{channelLabel} ({protocol}): {message}");
        }

        EmitSignal(SignalName.OnMessageRaw, channelId, data);
        EmitSignal(SignalName.OnMessageString, channelId, message);
    }

    /// <summary>
    /// ⚠️ Workaround: Must be called deferred.
    /// 
    /// Will emit the following Signals: 
    ///  - <see cref="OnChannelOpen"/>
    ///  - <see cref="OnChannelStateChange"/>
    /// <param name="channelId">The channel this got send</param>
    /// <param name="protocol">The protocol being used</param>
    /// <param name="data">The RAW (= byte[]) data send</param>
    private void signalOnChannelOpen(ushort channel)
    {
#if DEBUG
        var channelLabel = GetChannelLabel(channel);
        GD.Print($"[WebRTC] Channel #{channel}/{channelLabel} opened!");
#endif

        openChannels.Add(channel);

        EmitSignal(SignalName.OnChannelOpen, channel);
        EmitSignal(SignalName.OnChannelStateChange, channel, true);
    }

    /// <summary>
    /// ⚠️ Workaround: Must be called deferred.
    /// 
    /// Will emit the following Signals: 
    ///  - <see cref="OnChannelClose"/>
    ///  - <see cref="OnChannelStateChange"/>
    /// <param name="channelId">The channel this got send</param>
    /// <param name="protocol">The protocol being used</param>
    /// <param name="data">The RAW (= byte[]) data send</param>
    private void signalOnChannelClose(ushort channel)
    {
#if DEBUG
        var channelLabel = GetChannelLabel(channel);
        GD.Print($"[WebRTC] Channel #{channel}/{channelLabel} closed!");
#endif

        openChannels.Remove(channel);

        EmitSignal(SignalName.OnChannelClose, channel);
        EmitSignal(SignalName.OnChannelStateChange, channel, false);
    }
    #endregion

    #region Channel Methods
    public void SendVideo(uint durationRtpUnits, byte[] sample)
    {
        peer.SendVideo(durationRtpUnits, sample);
    }

    public void SendAudio(uint durationRtpUnits, byte[] sample)
    {
        peer.SendAudio(durationRtpUnits, sample);
    }

    private async Task<ushort> createChannel(string channelName, ushort channelId)
    {
        var channel = await peer.createDataChannel(channelName, new RTCDataChannelInit()
        {
            id = channelId,
            negotiated = true,
        }) ?? throw new InvalidChannelException(channelName, channelId);
        channels.Add(channelId, channel);

        // Signals
        channel.onopen += () =>
        {
            CallDeferred("signalOnChannelOpen", channelId);
        };
        channel.onclose += () =>
        {
            CallDeferred("signalOnChannelClose", channelId);
        };
        channel.onmessage += (channel, protocol, data) =>
        {
            CallDeferred("signalOnMessage", channel.id ??= 0, protocol.ToString(), data);
        };

        return channelId;
    }

    public bool DoesChannelExist(ushort channelId)
    {
        return channels.ContainsKey(channelId);
    }

    /// <summary>
    /// Sends a RAW (= byte[]) message.
    /// 
    /// <seealso cref="SendOnChannel(ushort, string)"/>
    /// </summary>
    /// <param name="channelId">The channel to send this on</param>
    /// <param name="data">The data to send</param>
    public void SendOnChannelRaw(ushort channelId, byte[] data)
    {
        var channel = GetChannelByID(channelId);
        channel.send(data);
    }

    /// <summary>
    /// Sends a string message.
    /// 
    /// <seealso cref="SendOnChannelRaw(ushort, byte[])"/>
    /// </summary>
    /// <param name="channelId">The channel to send this on</param>
    /// <param name="data">The data to send</param>
    public void SendOnChannel(ushort channelId, string message)
    {
        SendOnChannelRaw(channelId, message.ToUtf8Buffer());
    }

    /// <summary>
    /// Returns the label (= string) of a given channel
    /// </summary>
    /// <param name="channelId">The channel ID to probe</param>
    /// <returns>string containing the channel name</returns>
    public string GetChannelLabel(ushort channelId)
    {
        var channel = GetChannelByID(channelId);
        return channel.label;
    }

    /// <summary>
    /// Retrieves the channel ID by it's label (name).
    /// If possible, always use the ID of a channel or call this once, 
    /// then store the ID for future use.
    /// </summary>
    /// <param name="channelName">The name of the channel to lookup</param>
    /// <returns>The channel id of the channel</returns>
    public ushort GetChannelID(string channelName)
    {
        foreach (var (id, channel) in channels)
        {
            if (channel.label == channelName)
            {
                return id;
            }
        }

        throw new InvalidChannelException(channelName);
    }

    /// <summary>
    /// Gets a channel by it's ID.
    /// Always use this over <see cref="GetChannelByName(string)"/>!
    /// </summary>
    /// <param name="channelId">The ID of the channel to lookup.</param>
    /// <returns>The channel to be found</returns>
    /// <exception cref="InvalidChannelException">If a channel with that ID isn't found</exception>
    public RTCDataChannel GetChannelByID(ushort channelId)
    {
        RTCDataChannel channel;
        if (channels.TryGetValue(channelId, out channel))
        {
            return channel;
        }
        else
        {
            throw new InvalidChannelException(channelId);
        }
    }

    /// <summary>
    /// Gets a channel by it's name.
    /// This is inefficient, always use the channel ID with <see cref="GetChannelByID(ushort)"/> if possible!
    /// </summary>
    /// <param name="channelName">The name (label) of the channel to lookup.</param>
    /// <returns>The channel with that label.</returns>
    /// <exception cref="InvalidChannelException">If a channel with that name isn't found</exception>
    public RTCDataChannel GetChannelByName(string channelName)
    {
        var channelId = GetChannelID(channelName);
        return GetChannelByID(channelId);
    }

    /// <summary>
    /// Checks if a channel is open
    /// </summary>
    /// <param name="channelId">The channel to check</param>
    /// <returns>true, if the channel is open, false otherwise</returns>
    public bool IsChannelOpen(ushort channelId)
    {
        return GetChannelByID(channelId).IsOpened;
    }
    #endregion

    #region WebRTC
    /// <summary>
    /// Adds an ICE candidate to the peer
    /// </summary>
    /// <param name="init">The ICE candidate</param>
    public void AddICECandidate(RTCIceCandidateInit init)
    {
        peer.addIceCandidate(init);
    }

    /// <summary>
    /// Creates an offer.
    /// ⚠️ Only call this on a Host
    /// </summary>
    /// <returns>The offer</returns>
    public RTCSessionDescriptionInit CreateOffer()
    {
        if (!IsHost)
        {
            GD.PrintErr("WebRTCPeer::CreateOffer called on Non-Host!");
            return null;
        }

        return peer.createOffer(null);
    }

    /// <summary>
    /// Creates an answer.
    /// ⚠️ A remote description must be set with an offer (-> retrieved from a host, <see cref="CreateOffer"/>) first!
    /// ⚠️ Only call this on a Client.
    /// </summary>
    /// <returns>The offer</returns>
    public RTCSessionDescriptionInit CreateAnswer()
    {
        if (IsHost)
        {
            GD.PrintErr("WebRTCPeer::CreateAnswer called on Host!");
            return null;
        }

        return peer.createAnswer(null);
    }

    /// <summary>
    /// Sets a local session description.
    /// ⚠️ Never set this to a "remote" description.
    /// 
    /// Ideally, set this immediately after creating an Offer (<see cref="CreateOffer"/>) of Answer (<see cref="CreateAnswer"/>).
    /// </summary>
    /// <param name="sdp">The session to set</param>
    /// <returns>Async Task</returns>
    public async Task SetLocalDescription(RTCSessionDescriptionInit sdp)
    {
        await peer.setLocalDescription(sdp);
    }

    /// <summary>
    /// Sets a local session description.
    /// ⚠️ Never set this to a "local" description.
    /// 
    /// Ideally, set this immediately after receiving a session description from a peer.
    /// </summary>
    /// <param name="sdp">The session to set</param>
    /// <returns>Async Task</returns>
    public void SetRemoteDescription(RTCSessionDescriptionInit sdp)
    {
        peer.setRemoteDescription(sdp);
    }
    #endregion
}
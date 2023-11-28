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
    /// The, default, "main" channel ID.
    /// Every peer must have this.
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
    /// Whether the current peer is a host or not
    /// </summary>
    [Export]
    public bool IsHost = false;
    #endregion

    #region Fields
    /// <summary>
    /// The actual peer connection
    /// </summary>
    private RTCPeerConnection peer;
    /// <summary>
    /// The main channel.
    /// <seealso cref="MAIN_CHANNEL_ID"/>
    /// </summary>
    private RTCDataChannel mainChannel;
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
    #endregion

    #region Godot Functions
    public override async void _Ready()
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
        var config = new RTCConfiguration()
        {
            iceServers = rtcIceServers,
        };

        #region Peer
        // Create a new peer with the config
        peer = new RTCPeerConnection(config);

        // Signals
        peer.onicecandidate += (candidate) =>
        {
            CallDeferred("signalEmitterOnICECandidate", candidate.toJSON());
        };
        peer.oniceconnectionstatechange += (state) =>
        {
            CallDeferred("signalOnICEConnectionStateChangeEventHandler", state.ToString());
        };
        peer.onsignalingstatechange += () =>
        {
            CallDeferred("signalOnSignalingStateChangeEventHandler", peer.signalingState.ToString());
        };
        peer.onconnectionstatechange += (state) =>
        {
            CallDeferred("signalOnConnectionStateChangeEventHandler", state.ToString());
        };
        peer.onicegatheringstatechange += (state) =>
        {
            CallDeferred("signalOnGatheringStateChangeEventHandler", state.ToString());
        };

        GD.Print($"[WebRTC] Peer created!");
        #endregion

        #region Main Channel
        // Create main channel
        mainChannel = await peer.createDataChannel("main", new RTCDataChannelInit()
        {
            id = MAIN_CHANNEL_ID,
            negotiated = true,
        });
        if (mainChannel == null)
        {
            GD.PrintErr("[WebRTC] Main channel creation failed!");
            return;
        }

        // Signals
        mainChannel.onmessage += (channel, protocol, data) =>
        {
            CallDeferred("signalOnMessage", channel.id ??= 0, protocol.ToString(), data);
        };

        GD.Print($"[WebRTC] Main channel created!");
        #endregion
    }
    #endregion

    #region Signal Methods
    /// <summary>
    /// ⚠️ Workaround: Must be called deferred.
    /// 
    /// Will emit the <see cref="OnICECandidateJSON"/> signal.
    /// <param name="json">ICE Candidate as JSON</param>
    private void signalEmitterOnICECandidate(string json)
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

#if DEBUG
        var channelLabel = GetChannelLabel(channelId);
        GD.Print($"[WebRTC] Message on #{channelId}@{channelLabel} ({protocol}): {message}");
#endif

        EmitSignal(SignalName.OnMessageRaw, channelId, data);
        EmitSignal(SignalName.OnMessageString, channelId, message);
    }
    #endregion

    #region Methods
    /// <summary>
    /// Sends a RAW (= byte[]) message.
    /// 
    /// <seealso cref="SendOnChannel(ushort, string)"/>
    /// </summary>
    /// <param name="channelId">The channel to send this on</param>
    /// <param name="data">The data to send</param>
    public void SendOnChannelRaw(ushort channelId, byte[] data)
    {
        if (channelId != 0)
        {
            GD.PrintErr("[WebRTC] Invalid channel");
            return;
        }

        mainChannel.send(data);
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
        if (channelId != 0)
        {
            GD.PrintErr("[WebRTC] Invalid channel");
            return;
        }

        mainChannel.send(message.ToUtf8Buffer());
    }

    /// <summary>
    /// Returns the label (= string) of a given channel
    /// </summary>
    /// <param name="channelId">The channel ID to probe</param>
    /// <returns>string containing the channel name</returns>
    public string GetChannelLabel(ushort channelId)
    {
        if (channelId != 0)
        {
            GD.PrintErr("[WebRTC] Invalid channel");
            return "";
        }

        return "main";
    }

    /// <summary>
    /// Checks if a channel is open
    /// </summary>
    /// <param name="channelId">The channel to check</param>
    /// <returns>true, if the channel is open, false otherwise</returns>
    public bool IsChannelOpen(ushort channelId)
    {
        if (channelId != 0)
        {
            GD.PrintErr("[WebRTC] Invalid channel!");
            return false;
        }

        return mainChannel != null && mainChannel.IsOpened;
    }

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
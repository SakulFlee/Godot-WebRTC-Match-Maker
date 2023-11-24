using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using SIPSorcery.Net;

[GlobalClass]
public partial class WebRTCPeer : Node
{
    #region Globals
    public static readonly ushort MAIN_CHANNEL_ID = 0;
    #endregion

    #region Exports
    [Export]
    public Godot.Collections.Array ICEServers = new() {
        new Dictionary() {
            {"url", "turn:calamity-chicken.sakul-flee.de:3478"},
            {"username", "MPDungeon"},
            {"credential", "BS8om**B8R8WYPQ&NoiLq4T2jkphMS3*"}
        }
    };

    [Export]
    public bool IsHost = false;
    #endregion

    #region Fields
    private RTCPeerConnection peer;
    private RTCDataChannel mainChannel;
    #endregion

    #region Signals
    [Signal]
    public delegate void OnICECandidateJSONEventHandler(string json);

    [Signal]
    public delegate void OnICEConnectionStateChangeEventHandler(string state);

    [Signal]
    public delegate void OnSignalingStateChangeEventHandler(string state);

    [Signal]
    public delegate void OnConnectionStateChangeEventHandler(string state);

    [Signal]
    public delegate void OnGatheringStateChangeEventHandler(string state);

    [Signal]
    public delegate void OnMessageRawEventHandler(ushort channelId, byte[] data);

    [Signal]
    public delegate void OnMessageStringEventHandler(ushort channelId, string message);
    #endregion

    #region Godot Functions
    public override async void _Ready()
    {
        var rTCIceServers = new List<RTCIceServer>();
        foreach (Dictionary iceServer in ICEServers)
        {
            var rtcIceServer = new RTCIceServer
            {
                urls = (string)iceServer["url"],
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
            }
        }
        var config = new RTCConfiguration()
        {
            iceServers = rTCIceServers,
        };

#if DEBUG
        GD.Print($"RTC Config: {config}");
#endif

        #region Peer
        peer = new RTCPeerConnection(config);

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
        mainChannel = await peer.createDataChannel("main", new RTCDataChannelInit()
        {
            id = MAIN_CHANNEL_ID,
            negotiated = true,
        });

        mainChannel.onmessage += (channel, protocol, data) =>
        {
            CallDeferred("signalOnMessage", channel.id ??= 0, protocol.ToString(), data);
        };

        GD.Print($"[WebRTC] Main channel created!");
        #endregion
    }
    #endregion

    #region Signal Methods
    private void signalEmitterOnICECandidate(string json)
    {
#if DEBUG
        GD.Print($"[WebRTC]: ICE Candidate: {json}");
#endif

        EmitSignal(SignalName.OnICECandidateJSON, json);
    }

    private void signalOnICEConnectionStateChangeEventHandler(string state)
    {
#if DEBUG
        GD.Print($"[WebRTC]: ICE Connection State Changed: {state}");
#endif

        EmitSignal(SignalName.OnICEConnectionStateChange, state);
    }

    private void signalOnSignalingStateChangeEventHandler(string state)
    {
#if DEBUG
        GD.Print($"[WebRTC]: Signaling State Changed: {state}");
#endif

        EmitSignal(SignalName.OnSignalingStateChange, state);
    }

    private void signalOnConnectionStateChangeEventHandler(string state)
    {
#if DEBUG
        GD.Print($"[WebRTC]: Connection State Changed: {state}");
#endif

        EmitSignal(SignalName.OnConnectionStateChange, state);
    }

    private void signalOnGatheringStateChangeEventHandler(string state)
    {
#if DEBUG
        GD.Print($"[WebRTC]: Gathering State Changed: {state}");
#endif

        EmitSignal(SignalName.OnGatheringStateChange, state);
    }

    private void signalOnMessage(ushort channelId, string protocol, byte[] data)
    {
        var message = data.GetStringFromUtf8();

#if DEBUG
        var channelLabel = GetChannelLabel(channelId);
        GD.Print($"[WebRTC] Message on #{channelId}@{channelLabel} ({protocol}): {message}");
#endif

        EmitSignal(SignalName.OnMessageRaw, data);
        EmitSignal(SignalName.OnMessageString, message);
    }
    #endregion

    #region Methods
    public void SendOnChannelRaw(ushort channelId, byte[] data)
    {
        if (channelId != 0)
        {
            GD.PrintErr("[WebRTC] Invalid channel");
            return;
        }

        mainChannel.send(data);
    }

    public void SendOnChannel(ushort channelId, string message)
    {
        if (channelId != 0)
        {
            GD.PrintErr("[WebRTC] Invalid channel");
            return;
        }

        mainChannel.send(message.ToUtf8Buffer());
    }

    public string GetChannelLabel(ushort channelId)
    {
        if (channelId != 0)
        {
            GD.PrintErr("[WebRTC] Invalid channel");
            return "";
        }

        return "main";
    }

    public bool IsChannelOpen(ushort channelId)
    {
        if (channelId != 0)
        {
            GD.PrintErr("[WebRTC] Invalid channel!");
            return false;
        }

        return mainChannel.IsOpened;
    }

    public async Task<string> AutomatedBegin()
    {
        RTCSessionDescriptionInit sdp;
        if (IsHost)
        {
            sdp = CreateOffer();
        }
        else
        {
            sdp = CreateAnswer();
        }

        await SetLocalDescription(sdp);
        return sdp.toJSON();
    }

    public void AutomatedFinish(RTCSessionDescriptionInit sdp)
    {
        SetRemoteDescription(sdp);
    }

    public void AddICECandidate(RTCIceCandidateInit init)
    {
        peer.addIceCandidate(init);
    }

    public RTCSessionDescriptionInit CreateOffer()
    {
        if (!IsHost)
        {
            GD.PrintErr("WebRTCPeer::CreateOffer called on Non-Host!");
            return null;
        }

        return peer.createOffer(null);
    }

    public RTCSessionDescriptionInit CreateAnswer()
    {
        if (IsHost)
        {
            GD.PrintErr("WebRTCPeer::CreateAnswer called on Host!");
            return null;
        }

        return peer.createAnswer(null);
    }

    public async Task SetLocalDescription(RTCSessionDescriptionInit sdp)
    {
        await peer.setLocalDescription(sdp);
    }

    public void SetRemoteDescription(RTCSessionDescriptionInit sdp)
    {
        peer.setRemoteDescription(sdp);
    }
    #endregion
}
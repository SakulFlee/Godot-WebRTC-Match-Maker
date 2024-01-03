using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class WebRTCMultiplayerPeer : MultiplayerPeerExtension
{
    #region Fields: Peer Wrapper
    private PeerWrapper[] peerWrapper;

    internal class PeerWrapper
    {
        public int peerID;
        public WebRTCPeer peer;
        public bool connected = false;
    };
    #endregion

    #region Fields: Peer ID
    private int OwnID;
    #endregion

    #region Fields: Incoming
    private Queue<IncomingPacketWrapper> incomingPackets = new();

    internal class IncomingPacketWrapper
    {
        public int peerID;
        public int channelID;
        public byte[] data;
    }
    #endregion

    #region Fields: Outgoing
    private int outgoingPeerID;
    private ushort outgoingChannelID;
    #endregion

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="webRTCPeers">The previously established WebRTC peers</param>
    /// <param name="hostIndex">Which index of the peers is the host OR -1 if we/this is the host</param>
    public WebRTCMultiplayerPeer(WebRTCPeer[] webRTCPeers, int hostIndex)
    {
        // Ourself
        OwnID = hostIndex == -1 ? 1 : Random.Shared.Next();

        peerWrapper = new PeerWrapper[webRTCPeers.Length];
        for (uint i = 0; i < webRTCPeers.Length; i++)
        {
            var peer = webRTCPeers[i];
            var peerID = i == hostIndex ? 1 : Random.Shared.Next();

            var wrapper = new PeerWrapper()
            {
                peer = peer,
                peerID = peerID,
            };
            peerWrapper[i] = wrapper;

            // Called when the state of a channel changes.
            // A open channel implies the connection being stable.
            // The connection will only be marked as "connected" once the main
            // channel (id: 0) opened.
            peer.OnChannelStateChange += (channelId, isOpen) =>
            {
                if (channelId != 0)
                {
                    // We only care for the main channel, any additional channel is nice but not important for the connection status!
                    return;
                }

                if (isOpen)
                {
                    wrapper.connected = true;

                    // Signal a new connection opening (open channel implies connection is stable)
                    CallDeferred("signalPeerConnected", wrapper.peerID);
                }
                else
                {
                    wrapper.connected = false;

                    // Signal a connection being closed
                    CallDeferred("signalPeerDisconnected", wrapper.peerID);
                }
            };

            // Called when a new message is received.
            // Will add the message to the queue.
            peer.OnMessageRaw += (channelID, data) =>
            {
                incomingPackets.Enqueue(new IncomingPacketWrapper
                {
                    peerID = wrapper.peerID,
                    channelID = channelID,
                    data = data,
                });
            };
        }
    }

    // ⚠️ Workaround: Must be called with `CallDeferred` since signaling 
    // an event is only possible on a Godot thread. ⚠️
    private void signalPeerConnected(int peerId)
    {
        GD.Print($"[MatchMakerMultiplayerPeer] Signaling peer connected: {peerId}");
        EmitSignal(SignalName.PeerConnected, peerId);
    }

    // ⚠️ Workaround: Must be called with `CallDeferred` since signaling 
    // an event is only possible on a Godot thread. ⚠️
    private void signalPeerDisconnected(int peerId)
    {
        GD.Print($"[MatchMakerMultiplayerPeer] Signaling peer disconnected: {peerId}");
        EmitSignal(SignalName.PeerDisconnected, peerId);
    }

    public override void _Close()
    {
        foreach (var peer in peerWrapper)
        {
            peer.peer.peer.Close("_Close");
        }
        peerWrapper = null;
    }

    /// <summary>
    /// Disconnects a peer by it's ID
    /// There is no difference between a forced disconnect and a regular one.
    /// </summary>
    /// <param name="peerID">The peer ID to be disconnected</param>
    public override void _DisconnectPeer(int peerID, bool _)
    {
        peerWrapper.First(x => x.peerID == peerID).peer.peer.Close("_DisconnectPeer");
    }

    public override int _GetAvailablePacketCount()
    {
        return incomingPackets.Count();
    }

    public override ConnectionStatus _GetConnectionStatus()
    {
        return peerWrapper == null
            ? ConnectionStatus.Disconnected
            : peerWrapper.Length > 0
                ? ConnectionStatus.Connected
                : ConnectionStatus.Connecting;
    }

    public override int _GetMaxPacketSize()
    {
        uint minMessageSize = 0;
        foreach (var peer in peerWrapper)
        {
            var nestedPeer = peer.peer.peer;
            var maxMessageSize = nestedPeer.sctp.maxMessageSize;

            if (minMessageSize == 0 || maxMessageSize < minMessageSize)
            {
                minMessageSize = maxMessageSize;
            }
        }

        GD.PrintErr($"[MatchMakerMultiplayerPeer] Max packet size is set to the lowest message size based on SCTP: {minMessageSize}b");
        return (int)minMessageSize;
    }

    public override int _GetPacketChannel()
    {
        if (incomingPackets.Count == 0)
        {
            GD.PrintErr($"[MatchMakerMultiplayerPeer] _GetPacketChannel called when no incoming packets are available!");
            return -1;
        }

        // Only peek!
        var incomingPacket = incomingPackets.Peek();
        return incomingPacket.channelID;
    }

    /// <summary>
    /// Gets the current packet mode.
    /// Will always be "Reliable" due to WebRTC!
    /// </summary>
    /// <returns></returns>
    public override TransferModeEnum _GetPacketMode()
    {
        return TransferModeEnum.Reliable;
    }

    public override int _GetPacketPeer()
    {
        if (incomingPackets.Count == 0)
        {
            GD.PrintErr($"[MatchMakerMultiplayerPeer] _GetPacketPeer called when no incoming packets are available!");
            return -1;
        }

        // Only peek!
        var incomingPacket = incomingPackets.Peek();
        return incomingPacket.peerID;
    }

    public override byte[] _GetPacketScript()
    {
        if (incomingPackets.Count == 0)
        {
            GD.PrintErr($"[MatchMakerMultiplayerPeer] _GetPacketScript called when no incoming packets are available!");
            return Array.Empty<byte>();
        }

        // ONLY dequeue here! Peek everywhere else.
        var incomingPacket = incomingPackets.Dequeue();
        return incomingPacket.data;
    }

    /// <summary>
    /// Gets the current (free?) transfer channel.
    /// 0/Main is always free.
    /// </summary>
    /// <returns></returns> <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override int _GetTransferChannel()
    {
        return 0;
    }

    /// <summary>
    /// Gets the current transfer mode.
    /// Will always be Reliable due to WebRTC!
    /// </summary>
    /// <returns></returns>
    public override TransferModeEnum _GetTransferMode()
    {
        return TransferModeEnum.Reliable;
    }

    public override int _GetUniqueId()
    {
        return OwnID;
    }

    /// <summary>
    /// We always refuse new connections as this is 
    /// fully controlled by match maker!
    /// </summary>
    /// <returns></returns>
    public override bool _IsRefusingNewConnections()
    {
        return true;
    }

    public override bool _IsServer()
    {
        return OwnID == 1;
    }

    /// <summary>
    /// Returns if the server does support relaying.
    /// This is currently not implemented and not really needed as of now.
    /// Thus, this returns always false.
    /// </summary>
    /// <returns></returns>
    public override bool _IsServerRelaySupported()
    {
        return false;
    }

    /// <summary>
    /// Polls the server.
    /// Normally, this would be the place to check our sockets and handle some
    /// incoming and outgoing packet logic and such.
    /// However, our WebRTC implementation runs in a separate thread and is
    /// event based.
    /// This function does nothing, but needs to be implemented ... 
    /// </summary>
    public override void _Poll()
    { }

    public override Error _PutPacketScript(byte[] pBuffer)
    {
        peerWrapper.First(x => x.peerID == outgoingPeerID).peer.SendOnChannelRaw(outgoingChannelID, pBuffer);

        return Error.Ok;
    }

    /// <summary>
    /// Will do nothing. See <see cref="_IsRefusingNewConnections"/>.
    /// </summary>
    /// <param name="pEnable"></param>
    public override void _SetRefuseNewConnections(bool pEnable)
    { }

    public override void _SetTargetPeer(int peerID)
    {
        outgoingPeerID = peerID;
    }

    public override void _SetTransferChannel(int channelID)
    {
        if (channelID < 0)
        {
            GD.PrintErr("[MatchMakerMultiplayerPeer] Channel with negative ID supplied! Refusing invalid channel ID.");
            return;
        }

        if (channelID > ushort.MaxValue)
        {
            GD.PrintErr("[MatchMakerMultiplayerPeer] Channel ID that is bigger than ushort::MAX supplied! Refusing invalid channel ID.");
            return;
        }

        outgoingChannelID = (ushort)channelID;
    }

    /// <summary>
    /// Won't do anything. <see cref="_GetTransferMode" />
    /// </summary>
    /// <param name="mode"></param>
    public override void _SetTransferMode(TransferModeEnum mode)
    { }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class MatchMakerMultiplayerPeer : MultiplayerPeerExtension
{
    #region Fields: Match Maker
    private MatchMaker matchMaker;
    #endregion

    #region Fields: Peer ID and Peer UUID
    private int OwnID;

    private Dictionary<string, int> peerUUIDtoUniqueID = new();
    #endregion

    #region Fields: Logic
    private LinkedList<string> connectedPeers = new();
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
    private string outgoingPeerUUID;
    private ushort outgoingChannelID;
    #endregion

    public MatchMakerMultiplayerPeer(MatchMaker matchMaker)
    {
        this.matchMaker = matchMaker;

        // Called once a new connection is being made.
        // A new connection doesn't mean it's stable yet or ready to 
        // receive / send data.
        this.matchMaker.OnNewConnection += (peerUUID) =>
        {
            GD.Print($"[MatchMakerMultiplayerPeer] New connection: {peerUUID}");

            if (peerUUIDtoUniqueID.Count == 0)
            {
                // First connection!
                // Add ourself first. This cannot be done earlier as we don't know if we are a host or not.
                OwnID = matchMaker.IsHost ? 1 : Random.Shared.Next();
                addPeerUUIDtoIDTranslation(matchMaker.OwnUUID, OwnID);
            }

            // Add the other peer
            var otherID = peerUUID == matchMaker.HostUUID ? 1 : Random.Shared.Next();
            addPeerUUIDtoIDTranslation(peerUUID, otherID);
        };

        // Called when the state of a channel changes.
        // A open channel implies the connection being stable.
        // The connection will only be marked as "connected" once the main
        // channel (id: 0) opened.
        this.matchMaker.OnChannelStateChange += (peerUUID, channelId, isOpen) =>
        {
            if (channelId != 0)
            {
                // We only care for the main channel, any additional channel is nice but not important for the connection status!
                return;
            }

            if (connectedPeers.Contains(peerUUID))
            {
                // Skip if the peer is already contained (for some reason ...)
                return;
            }

            if (isOpen)
            {
                // Add to our list
                connectedPeers.AddLast(peerUUID);

                // Signal a new connection opening (open channel implies connection is stable)
                CallDeferred("signalPeerConnected", peerUUID);
            }
            else
            {
                // Remove from our list
                connectedPeers.Remove(peerUUID);

                // Signal a connection closing
                CallDeferred("signalPeerDisconnected", peerUUID);
            }
        };

        // Called when a new message is received.
        // Will add the message to the queue.
        this.matchMaker.OnMessageRaw += (peerUUID, channelID, data) =>
        {
            var peerID = getPeerIDfromPeerUUID(peerUUID);

            incomingPackets.Enqueue(new IncomingPacketWrapper
            {
                peerID = peerID,
                channelID = channelID,
                data = data,
            });
        };
    }

    // ⚠️ Workaround: Must be called with `CallDeferred` since signaling 
    // an event is only possible on a Godot thread. ⚠️
    private void signalPeerConnected(string peerUUID)
    {
        var id = peerUUIDtoUniqueID[peerUUID];

        GD.Print($"[MatchMakerMultiplayerPeer] Signaling peer connected: {peerUUID} - {id}");
        EmitSignal(SignalName.PeerConnected, id);
    }

    // ⚠️ Workaround: Must be called with `CallDeferred` since signaling 
    // an event is only possible on a Godot thread. ⚠️
    private void signalPeerDisconnected(string peerUUID)
    {
        var id = peerUUIDtoUniqueID[peerUUID];

        GD.Print($"[MatchMakerMultiplayerPeer] Signaling peer disconnected: {peerUUID} - {id}");
        EmitSignal(SignalName.PeerDisconnected, id);
    }

    private void addPeerUUIDtoIDTranslation(string peerUUID, int peerID)
    {
        peerUUIDtoUniqueID.Add(peerUUID, peerID);

        GD.Print($"[MatchMakerMultiplayerPeer] Added peer UUID to peer ID translation: {peerUUID} <-> {peerID}");
    }

    private string getPeerUUIDfromPeerID(int peerID)
    {
        return peerUUIDtoUniqueID.First(x => x.Value == peerID).Key;
    }

    private int getPeerIDfromPeerUUID(string peerUUID)
    {
        return peerUUIDtoUniqueID[peerUUID];
    }

    public override void _Close()
    {
        matchMaker = null;
    }

    /// <summary>
    /// Disconnects a peer by it's ID
    /// There is no difference between a forced disconnect and a regular one.
    /// </summary>
    /// <param name="peerID">The peer ID to be disconnected</param>
    public override void _DisconnectPeer(int peerID, bool _)
    {
        var peerUUID = getPeerUUIDfromPeerID(peerID);
        matchMaker.RemovePeer(peerUUID, "_DisconnectPeer");
    }

    public override int _GetAvailablePacketCount()
    {
        return incomingPackets.Count();
    }

    public override ConnectionStatus _GetConnectionStatus()
    {
        return matchMaker == null
            ? ConnectionStatus.Disconnected
            : connectedPeers.Count > 0
                ? ConnectionStatus.Connected
                : ConnectionStatus.Connecting;
    }

    public override int _GetMaxPacketSize()
    {
        uint minMessageSize = 0;
        foreach (var (_, peer) in matchMaker.webRTCConnections)
        {
            var nestedPeer = peer.peer;
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
        return matchMaker.IsHost;
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
        matchMaker.SendOnChannelRaw(outgoingPeerUUID, outgoingChannelID, pBuffer);
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
        var peerUUID = getPeerUUIDfromPeerID(peerID);
        if (peerUUID == null)
        {
            GD.PrintErr("[MatchMakerMultiplayerPeer] Invalid peer ID requested to be set as target!");
            return;
        }

        outgoingPeerUUID = peerUUID;
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
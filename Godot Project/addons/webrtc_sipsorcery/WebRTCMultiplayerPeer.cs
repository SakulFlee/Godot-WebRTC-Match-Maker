using System;
using System.Collections.Generic;
using System.Text;
using Godot;

public partial class WebRTCMultiplayerPeer : MultiplayerPeerExtension
{
    private WebRTCPeer[] webRTCPeers;
    private int peerID;

    private ConnectionStatus connectionStatus;

    private bool isHost;

    private Queue<Packet> incomingPacketQueue = new();
    private Packet currentIncomingPacket = new();

    private Queue<Packet> outgoingPacketQueue = new();
    private Packet currentOutgoingPacket = new();

    internal class Packet
    {

        public uint peerID;
        public uint channelID;
        public byte[] data;

        public Packet()
        {
            peerID = 0;
            channelID = 0;
            data = [];
        }

        public Packet(uint peerID, uint channelID, byte[] data)
        {
            this.peerID = peerID;
            this.channelID = channelID;
            this.data = data;
        }

        public override string ToString()
        {
            return $"Packet :: #{peerID}@{channelID} -> {Encoding.UTF8.GetString(data)} ({data.Length}b)";
        }
    }

    public WebRTCMultiplayerPeer(WebRTCPeer webRTCPeer)
    : this([webRTCPeer])
    {
    }

    public WebRTCMultiplayerPeer(WebRTCPeer[] webRTCPeers)
    {
        this.webRTCPeers = webRTCPeers;

        // Checks
        setupPeerCountSanityCheck();
        setupHostClientSanityCheck();

        if (isHost)
        {
            peerID = 1;
        }
        else
        {
            peerID = Random.Shared.Next() + 1;
        }

        // Signals
        setupOnMessageSignal();
        setupOnChannelListener();

        foreach (var peer in webRTCPeers)
        {
            EmitSignal(SignalName.PeerConnected, peerID);
        }
    }

    private void setupPeerCountSanityCheck()
    {
        if (webRTCPeers.Length == 0)
        {
            GD.PrintErr("[WebRTCPeerExt] Require at least one valid WebRTCPeer");
            throw new System.Exception("Require at least one valid WebRTCPeer");
        }
    }

    private void setupHostClientSanityCheck()
    {
        var hosts = 0;
        var clients = 0;
        foreach (var peer in webRTCPeers)
        {
            if (peer.IsHost)
            {
                hosts++;
            }
            else
            {
                clients++;
            }
        }
        if (!(hosts > 0 && clients == 0 || hosts == 0 && clients > 0))
        {
            GD.PrintErr("[WebRTCPeerExt] All peers must be in Host or Client mode. Mixed isn't allowed!");
        }
        isHost = hosts > 0;
    }

    private void setupOnMessageSignal()
    {
        uint counter = 1;
        foreach (var peer in webRTCPeers)
        {
            peer.OnMessageRaw += (_, data) =>
            {
                var localID = counter;

                incomingPacketQueue.Enqueue(
                    new Packet(localID, WebRTCPeer.MAIN_CHANNEL_ID, data)
                );
            };

            counter++;
        }
    }

    private void setupOnChannelListener()
    {
        connectionStatus = ConnectionStatus.Connecting;

        foreach (var peer in webRTCPeers)
        {
            peer.OnChannelStateChange += (channel, isOpen) =>
            {
                if (isOpen)
                {
                    connectionStatus = ConnectionStatus.Connected;
                }
                else
                {
                    connectionStatus = ConnectionStatus.Disconnected;
                }
            };
        }
    }

    public override void _Poll()
    {
        // GD.Print($"CALL: _Poll");

        // GD.Print($"TOutgoing: {outgoingPacketQueue.Count}");
        // GD.Print($"COutgoing: {currentOutgoingPacket}");
        // GD.Print($"TIncoming: {incomingPacketQueue.Count}");
        // GD.Print($"CIncoming: {currentIncomingPacket}");

        // TODO: Questions/Procedure
        // 1. Test with an existing implementation (e.g. ENet) if the demo actually works or if the issue is with something else
        // 2. Once the demo actually works, check if this works
        // 3. Check if we maybe are too fast? Signals aren't fired at all + constant polling of connection status. Godot may be expecting a change from "Connecting" to "Connected" or something. (Keep in mind that our connection _already exists_ once we initialize this!)

        foreach (var packet in outgoingPacketQueue)
        {
            webRTCPeers[packet.peerID - 1].SendOnChannelRaw((ushort)(packet.channelID - 1), packet.data);
        }
    }

    public override byte[] _GetPacketScript()
    {
        GD.Print($"CALL: _GetPacketScript");

        currentIncomingPacket = incomingPacketQueue.Dequeue();

        return currentIncomingPacket.data;
    }

    public override int _GetTransferChannel()
    {
        GD.Print($"CALL: _GetTransferChannel");

        return 1;
    }

    public override int _GetPacketChannel()
    {
        GD.Print($"CALL: _GetPacketChannel");

        return (int)currentIncomingPacket.channelID;
    }

    public override int _GetPacketPeer()
    {
        GD.Print($"CALL: _GetPacketPeer");

        return (int)currentIncomingPacket.peerID;
    }

    public override Error _PutPacketScript(byte[] pBuffer)
    {
        GD.Print($"CALL: _PutPacketScript");

        GD.Print($">>> {currentOutgoingPacket.peerID} - {currentOutgoingPacket.channelID} - {pBuffer.GetStringFromUtf8()}");

        if (currentOutgoingPacket.peerID == 0)
        {
            return Error.Unconfigured;
        }

        if (currentOutgoingPacket.channelID == 0)
        {
            return Error.Unconfigured;
        }

        currentOutgoingPacket.data = pBuffer;

        outgoingPacketQueue.Enqueue(currentOutgoingPacket);
        currentOutgoingPacket = new();

        return Error.Ok;
    }

    public override void _SetTargetPeer(int pPeer)
    {
        GD.Print($"CALL: _SetTargetPeer");

        currentOutgoingPacket.peerID = (uint)pPeer;
    }

    public override void _SetTransferChannel(int pChannel)
    {
        GD.Print($"CALL: _SetTransferChannel");

        currentOutgoingPacket.channelID = (uint)pChannel;
    }

    public override void _SetTransferMode(TransferModeEnum pMode)
    {
        GD.Print($"CALL: pMod");

        GD.Print("[WebRTCMultiplayerPeer] SetTransferMode is useless as WebRTC will always work in 'Reliable' mode!");
    }

    public override void _Close()
    {
        GD.Print($"CALL: _Close");

        foreach (var peer in webRTCPeers)
        {
            // Set status to disconnected
            // TODO: Closing peers?
        }
    }

    public override void _DisconnectPeer(int pPeer, bool pForce)
    {
        GD.Print($"CALL: _DisconnectPeer");

        // Set status to disconnected

        // TODO: Closing
    }

    public override ConnectionStatus _GetConnectionStatus()
    {
        // GD.Print($"CALL: _GetConnectionStatus");

        return connectionStatus;
    }

    public override int _GetMaxPacketSize()
    {
        GD.Print($"CALL: _GetMaxPacketSize");

        return 1200;
    }

    public override int _GetAvailablePacketCount()
    {
        // GD.Print($"CALL: _GetAvailablePacketCount");

        return incomingPacketQueue.Count;
    }

    public override TransferModeEnum _GetPacketMode()
    {
        GD.Print($"CALL: _GetPacketMode");

        return TransferModeEnum.Reliable;
    }

    public override TransferModeEnum _GetTransferMode()
    {
        GD.Print($"CALL: _GetTransferMode");

        return TransferModeEnum.Reliable;
    }

    public override int _GetUniqueId()
    {
        GD.Print($"CALL: _GetUniqueId");

        return peerID;
    }

    public override bool _IsRefusingNewConnections()
    {
        GD.Print($"CALL: _IsRefusingNewConnections");

        // Always refuse new connections from "external" sources
        return false; // TODO
    }

    public override bool _IsServer()
    {
        GD.Print($"CALL: _IsServer");

        GD.Print($">  Is Host: {isHost}");
        return isHost;
    }

    public override bool _IsServerRelaySupported()
    {
        GD.Print($"CALL: _IsServerRelaySupported");

        return false;
    }

    public override void _SetRefuseNewConnections(bool pEnable)
    {
        GD.Print($"CALL: _SetRefuseNewConnections");

        GD.Print("[WebRTCMultiplayerPeer] SetRefuseNewConnections is useless as this peer will never accept new external connections after initialization!");
    }
}

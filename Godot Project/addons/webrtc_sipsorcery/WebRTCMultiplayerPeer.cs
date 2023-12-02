using System.Collections.Generic;
using Godot;

public partial class WebRTCMultiplayerPeer : MultiplayerPeerExtension
{
    private WebRTCPeer[] webRTCPeers;

    private ConnectionStatus[] connectionStatuses;

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
    }

    public WebRTCMultiplayerPeer(WebRTCPeer webRTCPeer)
    {
        webRTCPeers = [webRTCPeer];
    }

    public WebRTCMultiplayerPeer(WebRTCPeer[] webRTCPeers)
    {
        this.webRTCPeers = webRTCPeers;

        // Checks
        setupPeerCountSanityCheck();
        setupHostClientSanityCheck();

        // Signals
        setupOnMessageSignal();
        setupOnChannelListener();
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
            throw new System.Exception("All peers must be in Host or Client mode. Mixed isn't allowed!");
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
        connectionStatuses = new ConnectionStatus[webRTCPeers.Length];

        uint counter = 1;
        foreach (var peer in webRTCPeers)
        {
            // Default value
            connectionStatuses[counter - 1] = ConnectionStatus.Connecting;

            peer.OnChannelStateChange += (channel, isOpen) =>
            {
                var localID = counter;

                var status = isOpen ? ConnectionStatus.Connected : ConnectionStatus.Connecting;
                connectionStatuses[localID] = status;
            };

            counter++;
        }
    }

    public override void _Poll()
    {
        foreach (var packet in outgoingPacketQueue)
        {
            webRTCPeers[packet.peerID - 1].SendOnChannelRaw((ushort)(packet.channelID - 1), packet.data);
        }
    }

    public override byte[] _GetPacketScript()
    {
        currentIncomingPacket = incomingPacketQueue.Dequeue();

        return currentIncomingPacket.data;
    }

    public override int _GetTransferChannel()
    {
        return (int)currentIncomingPacket.channelID;
    }

    public override int _GetPacketChannel()
    {
        return (int)currentIncomingPacket.channelID;
    }

    public override int _GetPacketPeer()
    {
        return (int)currentIncomingPacket.peerID;
    }

    public override Error _PutPacketScript(byte[] pBuffer)
    {
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
        currentOutgoingPacket.peerID = (uint)pPeer;
    }

    public override void _SetTransferChannel(int pChannel)
    {
        currentOutgoingPacket.channelID = (uint)pChannel;
    }

    public override void _SetTransferMode(TransferModeEnum pMode)
    {
        GD.Print("[WebRTCMultiplayerPeer] SetTransferMode is useless as WebRTC will always work in 'Reliable' mode!");
    }

    public override void _Close()
    {
        foreach (var peer in webRTCPeers)
        {
            // Set status to disconnected
            // TODO: Closing peers?
        }
    }

    public override void _DisconnectPeer(int pPeer, bool pForce)
    {
        // Set status to disconnected

        // TODO: Closing
    }

    public override ConnectionStatus _GetConnectionStatus()
    {
        var overallConnectionStatus = ConnectionStatus.Connected;

        foreach (var status in connectionStatuses)
        {
            if (status != ConnectionStatus.Connected)
            {
                overallConnectionStatus = status;
            }
        }

        return overallConnectionStatus;
    }

    public override int _GetMaxPacketSize()
    {
        return 1200;
    }

    public override int _GetAvailablePacketCount()
    {
        return incomingPacketQueue.Count;
    }

    public override TransferModeEnum _GetPacketMode()
    {
        return TransferModeEnum.Reliable;
    }

    public override TransferModeEnum _GetTransferMode()
    {
        return TransferModeEnum.Reliable;
    }

    public override int _GetUniqueId()
    {
        return 1; // TODO: ???
    }

    public override bool _IsRefusingNewConnections()
    {
        // Always refuse new connections from "external" sources
        return true;
    }

    public override bool _IsServer()
    {
        return isHost;
    }

    public override bool _IsServerRelaySupported()
    {
        return false;
    }

    public override void _SetRefuseNewConnections(bool pEnable)
    {
        GD.Print("[WebRTCMultiplayerPeer] SetRefuseNewConnections is useless as this peer will never accept new external connections after initialization!");
    }
}

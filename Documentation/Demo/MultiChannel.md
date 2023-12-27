# Demo: Multi Channel

The Multi Channel demo shows how multiple channels can be utilized.  
The demo itself is very basic, but shows the usage of channels.

![Demo: Multi Channel](../../.github/images/DemoMultiChannel.png)

## Details

Since a client will always ever have one connection (to the host), it can't choose a peer to send the message to.  
On the other hand, the host may have multiple connections and can choose the peer to send the message to.

The channel corresponds to a channel inside the WebRTC node.

Once a (for host: peer and) channel is picked, a payload message will be send to the peer.  
The peer will respond to it with what was received, on which channel and from where.

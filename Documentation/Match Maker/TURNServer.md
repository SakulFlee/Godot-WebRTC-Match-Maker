# Setting up a TURN Server

A TURN server is effectively a **relay**.
After a connection is properly made to it, a peer can send any data and the TURN server will simply relay it to the other clients.
This can be incredibly useful in cases where a P2P connection simply isn't possible due to network restraints and basically makes the difference between being unable to connect and having a connection.

Local connection pairs are always preferred over _relays_.
In fact, _relays_ always are the last to be checked.
[WebRTC] will try it's best to actually get a true P2P connection going without any _relays_.

> [!NOTE]  
> You can filter which candidate types are allowed to be added to each peers via the `CandidateFilter`.

Now, once again, there is a decision here:
Do you need a TURN server?  
If your game is only played locally inside the same network, say some Nintendo-isk party game, **you won't need this**. Local connections should almost always succeed. (Though you may also not need all of this in the first place. Broadcasting WebRTC candidates via e.g. UDP inside your network would have about the same effect as the whole _Match Maker Server_, minus the _Match Making_).

However, if your game is played basically "online" or "around the world" you absolutely need this, OR, account for a high chance of connections failing.

You can use **any** TURN server, configure it properly and set the correct details in Godot under the ICE Servers setting.  
However, I would highly recommend [CoTURN] given I am using it for developing this project and my games.  
Check their documentation!
It's incredibly easy to setup and you basically only need to add a username and password (+ set that in Godot!), maybe some firewall whitelisting and you are done.

[WebRTC]: https://webrtc.org/
[CoTURN]: https://github.com/coturn/coturn

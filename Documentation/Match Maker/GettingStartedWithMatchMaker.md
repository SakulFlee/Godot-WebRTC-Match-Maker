# Getting started with Match Maker server

To acquire the Match Maker server there are multiple ways:

- Clone the repository and build from source
- Download ZIP and build from source
- Download a [Release](releases/)
- ... and possible more ...

## Building from source

> If you downloaded a release, skip to the next section!

If you decided to download the source code/build from source, you will need [Rust](https://www.rust-lang.org/) installed.  
Ideally, use [RustUp](https://rustup.rs/) for this.
It will guide you through everything you need to know :)

Go into the _Match Maker_ directory and build the project with:

```bash
cargo build --release
```

> Make sure to include the release flag for compiler optimizations and faster executables!

You'll find the server binary under `Match Maker Server/target/release/match_maker_server(.exe)`.

Alternatively, you can use the following to directly build **and run** the project:

```bash
cargo run --release
```

> Make sure to include the release flag like stated above.

The server should be running now!

## Godot setup

Now that the server can be run, you want to go back to your Godot project and change the connection string of the `MatchMaker` node.

If the server is running locally, the default connection string (`ws://127.0.0.1:33333`) should suffice.  
If you happen to install this on a dev-server or similar, you will need to change this to match your servers IP address and port.

> Also make sure the given port is actually opened in your firewall.

In most cases, you probably want to setup two or three servers:

1. One locally, for local quick testing.
2. (Optional) One dev-server, for testing with other team members.
3. One production-server, for a released game.

> ![TIP]  
> Oracle Cloud offers a [free tier] which can be perfectly utilized as a dev- and possibly even a (small-scale?) production-server.

## Server configuration

When running the server for the first time, a configuration file will be created.
The location will be shown in your console!

It should be looking something like this:

```toml
listen_address = '0.0.0.0'  (1)
listen_port = 33333         (2)

[slots]
PingPong = 2                (3)
(...)
```

1. Will define the IP address the server will listen on.  
   `0.0.0.0` means 'any IP address'.
   You may want to set this to your public IP address.

1. Will define the port the server will listen on.  
   Make sure this port is actually opened in your firewall!

1. Lastly, you can add many slot configurations in this config.  
   The syntax is: `name = slot count`.  
   The name must match the name send in the `MatchMakerRequest` packet by your game.  
   The slot count must be reached for a game to start.  
   Meaning, if you set this to e.g. `8`, then 8x peers will have to connect to the server **and** request to join that room until it can start.
   The game won't start with only 7 peers.  
   The game also won't start with 8x peers, but only 7x requesting the same room.

## Better connectivity

This project tries to always establish a local direct connection between peers where possible.  
However, in some cases this simply isn't possible.  
Networks can be quite different and will have different security rules in-place.  
This makes direct connections essentially impossible in some very strict networks.

Telling users to decrease their network security is a very bad practice and should be avoided at all costs.  
Thus, another solution is needed.

When [WebRTC] starts it's initial setup, it will contact a so called [STUN] server.  
The [STUN] server will identify multiple possible connection routes to the requesting peer.  
Said routes are called _ICE Candidates_.

[STUN] stands for "Session Traversal Utilities for NAT".  
However, there is an upgraded version of a [STUN] server called [TURN].  
[TURN] stands for "Traversal Using Relays around NAT".

> NAT stands for "Network Address Translation"

A [TURN] server is basically a [STUN] server, but it can also relay information between peers.  
When a given peer contacts a [TURN] server, instead of a [STUN] server, additional "relay" _ICE Candidates_ will be added.  
Those "relay" candidates should always be a last resort as we are trying to eliminate the need for a central server with [WebRTC].
Yet, we are having a central server as a fallback here.

Hosting a [TURN] server for your project will add cost and complexity, but will ensure that connections between peers will always be possible.
Even behind very restrictive networks.

Checkout the \_[setting up a TURN Server](./TURNServer.md) guide if you want to setup one without much hassle.

> ![TIP]  
> Oracle cloud offers a [free tier] which can be utilized to host such a [TURN] server!  
> Both, the _Match Maker Server_ and a [TURN] server can be hosted on the same server!

[WebRTC]: https://webrtc.org/
[STUN]: https://en.wikipedia.org/wiki/STUN
[TURN]: https://en.wikipedia.org/wiki/Traversal_Using_Relays_around_NAT
[free tier]: https://www.oracle.com/cloud/free/

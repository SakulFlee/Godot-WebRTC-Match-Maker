# Godot WebRTC Match Maker | WebRTC P2P Match Maker for Godot

[![Godot - 4.x](https://img.shields.io/badge/Godot-4.x-53a4e0?style=for-the-badge&logo=godotengine&logoColor=53a4e0)](https://godotengine.org)
[![Rust - 1.63.0+](https://img.shields.io/badge/Rust-1.63.0+-e43716?style=for-the-badge&logo=Rust&logoColor=FFFFFF)](https://www.rust-lang.org/)
[![.NET - 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=csharp)](https://www.rust-lang.org/)
[![WebRTC](https://img.shields.io/badge/WebRTC-0943a2?style=for-the-badge&logo=webrtc)](https://webrtc.org)

- [Godot WebRTC Match Maker | WebRTC P2P Match Maker for Godot](#godot-webrtc-match-maker--webrtc-p2p-match-maker-for-godot)
  - [What is this?](#what-is-this)
  - [Current Limitations](#current-limitations)
  - [How to use this?](#how-to-use-this)
    - [Setting up Godot](#setting-up-godot)
      - [Adding to an existing project / Creating a new project](#adding-to-an-existing-project--creating-a-new-project)
      - [Using the example project](#using-the-example-project)
      - [Back to Godot](#back-to-godot)
    - [Setting up the Match Maker Server](#setting-up-the-match-maker-server)
    - [Setting up a TURN Server (Optional)](#setting-up-a-turn-server-optional)
  - [How does this work?](#how-does-this-work)
  - [Usage in non-Godot projects](#usage-in-non-godot-projects)
  - [Contributing](#contributing)
  - [License](#license)

## What is this?

This repository holds two projects that work together to achieve **true** P2P ("peer-to-peer", i.e. direct connections between peers without a server in-between _if possible_) between two or more peers.

The first project is the _Match Maker Server_.  
It's written in [Rust] and utilizes WebSockets for Client (Game / Godot) to Server communication.
This server will be utilized for _Match Making_ and as a peer relay.

> [!NOTE]  
> While the WebRTC P2P is true, we still need a so called "Signaling Server" to share required information between both peers to establish a WebRTC connection.
> Once the WebRTC connection is made the WebSocket connection to the _Match Maker Server_ can be dropped!

The second project is a _[Godot] example project_, containing two Plugins: _[WebRTC](Godot%20Project/addons/webrtc_sipsorcery/)_ and _[Match Maker for Godot](Godot%20Project/addons/match_maker/)_.
This _example project_ can be used as a starting template.
It features multiple demos to showcase how this whole project is working together.
Alternatively, you can also grab the plugins and use them in your already existing project!

Check _[how to use this](#how-to-use-this)_ for more information on both approaches.

> [!NOTE]  
> While these plugins are written for [Godot], nothing is stopping you from using this in another project or Engine.
> If you happen to port this over to some other Engine or Framework: Please share your work!  
> I would love to have multiple engines integrated with this.
>
> Check _[usage in non-Godot projects](#usage-in-non-godot-projects)_ for more.

## Current Limitations

This project (and any part of it) are still WIP (work-in-progress).  
They are useable, but some limitations need to be known:

1. Only a connection between **two** peers is possible at this moment.  
There are still a lot of issues with more than two peers and most, if not all, demo projects will not work straight away with more than two peers.  
Currently, we think around milestone v2.0.0 this limitation should be removed.  
Related issue: [#17](https://github.com/SakulFlee/Godot-WebRTC-Match-Maker/issues/17)

1. Only low-level networking is possible at this moment.  
This should be fine for most, but if you are expecting to use Godot's high-level multiplayer classes (like `MultiplayerSpawner` & `MultiplayerSynchronizer`) **this is currently not possible.**  
We are aiming to add this at some later point, probably around milestone v3.0.0.  
Related issue: [#79](https://github.com/SakulFlee/Godot-WebRTC-Match-Maker/issues/79)

1. Android support is limited at best.  
The project builds, exports and opens on the target device, but selecting any demo will crash the app.
The cause for this is as of writing this still unknown but seems to be a .NET issue, rather than a Godot issue.
Either way, Android support in Godot 4 is (as of writing this) new and experimental.
Furthermore, there seem to be an issue with 32-bit-based builds. 64-bit-based builds properly!  
Related issue: [#106](https://github.com/SakulFlee/Godot-WebRTC-Match-Maker/issues/106)

1. iOS support is unknown.  
I currently don't have any way of testing iOS (and partially macOS) builds.  
**We require testers to validate this limitation. It may just work fine. Come find out and tell us about it!**  
Related issue: [#107](https://github.com/SakulFlee/Godot-WebRTC-Match-Maker/issues/107)

1. Web isn't supported (yet).  
While Godot 4 can export to the web, it cannot export C# projects to the web.  
This whole project is using C# and there is now way around it.  
Thus, until Godot 4 supports exporting to the Web with C#, this won't work on the web.  
**However! Godot 4 has an official WebRTC client _exclusively for Web exports_ build in!**  
Related issue: [#108](https://github.com/SakulFlee/Godot-WebRTC-Match-Maker/issues/108) | Godot Docs: [Godot WebRTC Networking](https://docs.godotengine.org/en/stable/tutorials/networking/webrtc.html#using-webrtc-in-godot)

1. Audio/Video demo being broken.  
The Audio/Video demo is using parts of SIPSorcery which are still in a pre-release phase.
Thus, stuff may break and in-fact seems to break on most systems.
To get the demo fully working you will have to build your own `vpxmd` library for your specific platform and include it in the project.  
This should be resolved once SIPSorcery finalizes and actually releases their library.  
**For this reason, the demo is incomplete as of now as it doesn't properly stream the test file and doesn't display that inside Godot.**  
In the meantime/Alternatively, all it's doing is utilizing a data channel with some extra events.
You can very easily send or stream any audio/video file over a normal data channel.  
Related issue: [#109](https://github.com/SakulFlee/Godot-WebRTC-Match-Maker/issues/109)

## How to use this?

One of the goals of this project is ease-of-use.  
Thus, usage of this project should be as simple as possible.

However, another goal of this project is customizability.  
Thus, multiple options are available.  
Pick what fits your use-case the best.

### Setting up [Godot]

Firstly, decide if you want to add this to an existing project, make a new project, or, use the included example project for a quick-start.

#### Adding to an existing project / Creating a new project

> [!WARNING]  
> Make sure your [Godot] project **is a C# enabled** project and that your [Godot] version comes with **C# support enabled** (commonly referred to as `Godot.Mono`).  
> Also, check your .NET SDK version.
> This project uses **.NET/C# 8.0+**.
> Both, your local `.csproj` and installed SDK must honor this.

You have multiple options on how to add this to your project:

- via Asset Library
- via downloading a [Release](releases/)
- via cloning this Repository
- via downloading a ZIP from GitHub
- ... and possibly more ...

Chose whatever fits your use-case the best.  
The important part is that [Godot] expects the files under `/addons/**/*`.

Secondly, you will have to install a .NET library like so:

```bash
dotnet add package SIPSorcery
```

This will install all required libraries for you.

Now, make a new scene, or use an existing one, and add the `MatchMaker` node to it.

Once the `MatchMaker` node is added to your scene check the Inspector panel and set the `Match Maker Connection String`.
We will set this up in the next section, **remember to come back here**!

> The connection string is expected to be in the following format:  
> ws://[ip address or domain]:[port]  
>
> If you host the server locally (see below), it would be:  
> ws://127.0.0.1:33333

Lastly, you will need to interface with the `MatchMaker`.  
To do so: Add a script to your scene and get the `MatchMaker` node.
Then, use `MatchMaker::SendRequest` with a `MatchMakerRequest` to send a request to the server.  
An example implementation in C# may look like this:

![Scene Example](.github/images/scene_example.png)

```csharp
// Main.cs
using Godot;

public partial class Multiplayer : Node
{
    private MatchMaker matchMaker;
    private bool requestSend = false;

    public override void _Ready()
    {
        // (1)
        matchMaker = GetNode<MatchMaker>("MatchMaker");
    }

    public override void _Process(double delta)
    {
        // (2+3)
        if (!requestSend && matchMaker.IsReady())
        {
            // (4)
            var error = matchMaker.SendRequest(new MatchMakingRequest()
            {
                name = "Test",
            });
            // (5)
            requestSend = error == Error.Ok;
        }
    }
}
```

The above will do:

1. Get the node `MatchMaker` we added to the scene tree
2. If we haven't send a request yet:
3. Check if the `MatchMaker` is ready, if so:
4. Attempt sending our request (`MatchMakingRequest`)
5. Check for the `Error`. If it failed to send the procedure is repeated. Otherwise, mark the request as send.

Continue at _[back to Godot](#back-to-godot)_.

#### Using the example project

You have multiple options on how to get the _example project_ going:

- via Asset Library
- via downloading a [Release](releases/)
- via cloning the Repository
- via downloading a ZIP from GitHub
- ... and possibly more ...

Chose whatever fits your use-case the best.  
Continue at _[back to Godot](#back-to-godot)_.

#### Back to [Godot]

Now, that we have a project setup, you **must** hit the _Build_ button inside [Godot] **at least once**.
[Godot] needs to compile the plugins before we are able to activate them!  
Once the _build_ succeeded head to: Project (top left inside [Godot]) -> Project Settings -> Plugins (tab atop) and **Enable** both the _"Match Maker"_ and _"WebRTC (SIPSorcery)"_ plugins.

> [!CAUTION]  
> If enabling the plugins fails for any reason try to re-building the project and restarting [Godot]!

That's it! 🎉  
[Godot] should now be setup and able to use the _Match Maker_.

> Make sure to have the correct settings in the `MatchMaker` node!

Continue with _[setting up the Match Maker Server](#setting-up-the-match-maker-server)_ to learn more about hosting the _Match Maker Server_.

### Setting up the Match Maker Server

Make sure [Rust] is installed.  
If it isn't installed, ideally use [RustUp].

Checkout the repository and go into the _Match Maker Server_ directory.

Now, build the project with:

```bash
cargo build --release
```

> Make sure to include the release flag!

You'll find the server binary under `Match Maker Server/target/release/match_maker_server(.exe)`.

Alternatively, you can use the following to directly build and run the project:

```bash
cargo run --release
```

> Make sure to include the release flag!

The server should be running now!  
**Head back to your Godot project and add the connection string.**  
If the server is running locally on the same device add `ws://127.0.0.1:33333` as the connection string.

> ![TIP]
> The default logging level is set to `info`.  
> If you want more insights into what packages are received set it to `debug` via the `RUST_LOG` environment variable (`RUST_LOG=debug cargo run`).

However, _at least for released games_, it is highly recommended to actually host this server somewhere.  
As a quick and free server you can check out [Oracle Cloud Free-Tier].  
Simply follow the same steps of installing [Rust], compiling as release and run it.

When running the server for the first time, a configuration file will be created.
The location will be shown in your console/terminal.
Change this config to your needs.

Lastly, checkout _[setting up a TURN Server](#setting-up-a-turn-server-optional)_ if you want or need reliable connections in scenarios where P2P-Direct connections aren't possible.

### Setting up a TURN Server (Optional)

**This step is optional**.
However, if you don't have this many P2P connections will likely fail.
In my opinion, it's best to have this at least as a backup.
Since you are already hosting the _Match Maker Server_, you can easily host a TURN server aside there too!

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

You can use **any** TURN server, configure it properly and set the correct details in [Godot] under the ICE Servers setting.  
However, I had great success with [CoTURN], thus my recommendation for it.
This can be done on the same server as the Match Maker!

If you decide to go with [CoTURN]: Check their documentation. It's incredibly easy to setup and you basically only need to add a username and password (+ set that in [Godot]!).

## How does this work?

This project aims to be simple.  
There are more details hidden, but let's focus on the important bits:

> We differentiate between `clients` ("joins a game") and `hosts` ("hosts a game").  
> Both are named `peer` here.

First, a peer connects to the Match Maker via a WebSocket.  
Once a connection is opened, the peer will send a `MatchMakingRequest` to the server.

This `MatchMakingRequest` contains some basic information about the game.
Such as, what map/level/scene is being attempted to play.

The peer now waits until the room is full.  
In the meantime, another peer connects, following the same procedure, and fills the room.

Both clients now receive a `MatchMakingResponse`.  
This response includes whether the given peer is assigned as a Host (typically the first to create/join the room) or a Client, as well as a list of peers to connect to.

Each peer now initializes the [WebRTC] backend.  
This includes creating and setting a local session.

The client now sends their session description to the host via the Match Making server.  
Once received, the host sets this session description as the remote session and creates an offer.

In the process of creating offers, ICE Candidates will be generated.  
These candidates will be send to the client, once again via the Match Making server.

Once this succeeded, both peers should be able to connect to each other.

Here is a _simplified_ overview:

![Overview](Overview.drawio.svg)

## Usage in non-Godot projects

> Read through [How does this work?](#how-does-this-work) first!

Effectively, as a starting point you should checkout all the C# classes from both plugins:

- _[WebRTC SIPSorcery](Godot%20Project/addons/webrtc_sipsorcery/)
- _[Match Maker](Godot%20Project/addons/match_maker/)

There are _some_ [Godot] specific things, like Signals, but most of this you should be able to easily reuse with some tweaks.  
Signals, for example, can probably be exchanged for Async-Tasks.

Follow the existing implementation in Godot:

1. Open a WebSocket connection to the Match Making server
2. Send a `MatchMakingRequest`
3. Wait for `MatchMakingResponse` and parse it
4. Create a WebRTC peer for each peer listed
5. _Session part_
   1. If host: Create Offer, set it as local session & send it to the other peer
   2. If client: Wait for a Offer, set it as remote session, create Answer & send it to host back
   3. If host: Wait for Answer, set it as remote session
6. For both (client & host): Share ICE Candidates with each other via relay
7. Wait for peers to be connected and connection to be stable

## Contributing

Contributions of any kind are more than welcome!  
Please open [issues](issues/) for bugs, problems and feature requests.

Furthermore, we are especially looking for security experts to _"patch some holes"_.
The current _Match Maker Server_ **does** work, but could be a security liability.

If you end up porting this to another Engine, Framework or Project, please open an [issue](issues/) to merge it into this repository for everyone.
Alternatively, we could link to your repository, however merging would be highly appreciated!

## License

This repository (includes BOTH projects) is licensed under the MIT License.  
Essentially, do whatever you want with this :)

However, [contributions](#contributing) are highly appreciated!  
Please don't hesitate to reach out to me.

[WebRTC]: https://webrtc.org/
[Rust]: https://www.rust-lang.org/
[RustUp]: https://rustup.rs/
[Oracle Cloud Free-Tier]: https://www.oracle.com/cloud/free/
[Coturn]: https://github.com/coturn/coturn
[Godot]: https://godotengine.org/

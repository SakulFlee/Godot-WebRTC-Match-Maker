# TODO

- Update images

# Demo: Game

The game demo is the most complex demo we currently have.  
Multiple peers can connect to the same game session and will be spawned as a controllable character inside the game world.

![Demo: Game](../../.github/images/DemoGame.png)

## Classes

Due to the complexity of this demo (and scalability in the future!), multiple classes are required this time.  
Let's go over them:

| Name            | Description                                                                                                                                               |
| --------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Game.cs`       | The main class of the demo. Initializes the Match Maker connection and game world.                                                                        |
| `GamePacket.cs` | Since we are sending more complex and different data this time, we need a way to send actual game packets. This class acts as a base for any game packet. |

## Details

TODO

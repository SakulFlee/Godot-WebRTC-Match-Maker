using Godot;

public class GamePacketPlayerMove
{
    public string PeerUUID;
    public Vector2 Position;
    public override string ToString()
    {
        return $"GamePacketPlayerMove :: Position: {Position}";
    }
}
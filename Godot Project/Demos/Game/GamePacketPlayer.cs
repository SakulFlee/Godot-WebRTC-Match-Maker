using Godot;

public class GamePacketPlayer
{
    public string PlayerUUID;
    public Vector2 Position;
    
    public override string ToString()
    {
        return $"GamePacketPlayerMove :: Position: {Position}";
    }
}
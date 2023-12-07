using Godot;

public class GamePacketAddPlayer
{
    public string Label;
    public Vector2 Position;

    public override string ToString()
    {
        return $"GamePacketAddPlayer :: Label: {Label}, Position: {Position}";
    }
}
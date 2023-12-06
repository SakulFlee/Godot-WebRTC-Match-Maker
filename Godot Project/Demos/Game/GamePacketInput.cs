using Godot;

public class GamePacketInput
{
    public Vector2 InputVector;
    public override string ToString()
    {
        return $"GamePacketInput :: Input Vector: {InputVector}";
    }
}
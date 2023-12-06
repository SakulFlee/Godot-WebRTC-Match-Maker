public class GamePacketAddPlayer
{
    public string Label;
    public override string ToString()
    {
        return $"GamePacketAddPlayer :: Label: {Label}";
    }
}
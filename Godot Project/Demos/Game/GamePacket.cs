using System;

public class GamePacket
{
    public GamePacketType Type;
    public string InnerJSON;

    public GamePacket()
    { }

    public GamePacket(GamePacketType type, string innerJSON)
    {
        Type = type;
        InnerJSON = innerJSON;
    }

    public GamePacket(GamePacketType type, object inner)
    : this(type, PacketSerializer.ToJSON(inner))
    { }

    public T InnerAs<T>()
    {
        return PacketSerializer.FromJSON<T>(InnerJSON);
    }

    public void SetInner(object inner)
    {
        InnerJSON = PacketSerializer.ToJSON(inner);
    }

    public static GamePacket FromJSON(string json)
    {
        return PacketSerializer.FromJSON<GamePacket>(json);
    }

    public string ToJSON()
    {
        return PacketSerializer.ToJSON(this);
    }
    public override string ToString()
    {
        return $"GamePacket@{Type}";
    }
}
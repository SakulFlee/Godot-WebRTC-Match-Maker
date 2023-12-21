public class ChatMessagePacket
{
    public string fromUUID;
    public ushort channel;
    public string messsage;

    public static ChatMessagePacket FromJSON(string json)
    {
        return PacketSerializer.FromJSON<ChatMessagePacket>(json);
    }

    public string ToJSON()
    {
        return PacketSerializer.ToJSON(this);
    }
}

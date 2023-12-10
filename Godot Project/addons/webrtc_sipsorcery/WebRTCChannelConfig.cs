using Godot;

[GlobalClass]
public partial class WebRTCChannelConfig : Resource
{
    [Export]
    public string ChannelName;

    [Export]
    public WebRTCChannelType Type;
}
#if TOOLS
using Godot;

[Tool]
public partial class WebRTCPlugin : EditorPlugin
{
	public override void _EnterTree()
	{
		GD.Print("[WebRTC] Plugin loaded!");
	}
}
#endif

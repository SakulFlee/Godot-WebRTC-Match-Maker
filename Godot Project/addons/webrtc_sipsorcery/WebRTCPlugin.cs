#if TOOLS
using Godot;

/// <summary>
/// The WebRTC plugin for Godot.
/// </summary>
[Tool]
public partial class WebRTCPlugin : EditorPlugin
{
	public override void _EnterTree()
	{
		GD.Print("[WebRTC] Plugin loaded!");
	}
}
#endif

#if TOOLS
using Godot;

/// <summary>
/// The Match Maker plugin for Godot.
/// </summary>
[Tool]
public partial class MatchMakerPlugin : EditorPlugin
{
	public override void _EnterTree()
	{
		GD.Print("[MatchMaker] Plugin loaded!");
	}
}
#endif

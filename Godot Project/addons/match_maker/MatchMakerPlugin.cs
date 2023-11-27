#if TOOLS
using Godot;

[Tool]
public partial class MatchMakerPlugin : EditorPlugin
{
	public override void _EnterTree()
	{
		GD.Print("[MatchMaker] Plugin loaded!");
	}
}
#endif

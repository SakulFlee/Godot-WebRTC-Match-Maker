using Godot;

[Tool]
public partial class TestClass : Node
{
    public override void _EnterTree()
    {
        GD.Print("ENTER TREE");
    }
}
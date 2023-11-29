using Godot;

public partial class Main : Control
{
	public void OnPingPongButton()
	{
		var err = GetTree().ChangeSceneToFile("res://Demos/PingPong/PingPong.tscn");
		if (err != Error.Ok)
		{
			GD.PrintErr($"Failed to switch scene ({err})");
			return;
		}
	}

	public void OnChatButton()
	{
		var err = GetTree().ChangeSceneToFile("res://Demos/Chat/Chat.tscn");
		if (err != Error.Ok)
		{
			GD.PrintErr($"Failed to switch scene ({err})");
			return;
		}
	}

	public void OnGameButton()
	{
		var err = GetTree().ChangeSceneToFile("res://Demos/Game/Game.tscn");
		if (err != Error.Ok)
		{
			GD.PrintErr($"Failed to switch scene ({err})");
			return;
		}
	}
}

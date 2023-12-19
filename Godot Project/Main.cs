using Godot;

public partial class Main : Panel
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

	public void OnMultiChannelButton()
	{
		var err = GetTree().ChangeSceneToFile("res://Demos/MultiChannel/MultiChannel.tscn");
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

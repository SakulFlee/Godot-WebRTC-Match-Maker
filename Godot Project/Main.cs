using Godot;

public partial class Main : Panel
{
	private void switchScene(string path)
	{
		var err = GetTree().ChangeSceneToFile(path);
		if (err != Error.Ok)
		{
			GD.PrintErr($"Failed to switch scene ({err})");
			return;
		}
	}

	public void OnPingPongButton()
	{
		switchScene("res://Demos/PingPong/PingPong.tscn");
	}

	public void OnChatButton()
	{
		switchScene("res://Demos/Chat/Chat.tscn");
	}

	public void OnMultiChannelButton()
	{
		switchScene("res://Demos/MultiChannel/MultiChannel.tscn");
	}

	public void OnGameButton()
	{
		switchScene("res://Demos/Game/Game.tscn");
	}

	public void OnVideoCallButton()
	{
		switchScene("res://Demos/VideoCall/VideoCall.tscn");
	}

	public void OnHighLevelButton()
	{
		switchScene("res://Demos/HighLevel/HighLevel.tscn");
	}
}

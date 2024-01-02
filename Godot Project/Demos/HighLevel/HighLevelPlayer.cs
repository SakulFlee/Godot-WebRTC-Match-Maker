using System;
using Godot;

public partial class HighLevelPlayer : CharacterBody2D
{
	public const float Speed = 16000.0f;

	private int id;

	public override void _EnterTree()
	{
		id = int.Parse(Name);
		GetNode<Label>("IDLabel").Text = Name;
		SetMultiplayerAuthority(id);

		if (IsMultiplayerAuthority())
		{
			var random = new Random();
			Position = new Vector2(
				random.Next(0, 500),
				random.Next(0, 500)
			);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsMultiplayerAuthority())
		{
			Vector2 inputVector = Input.GetVector("Move Left", "Move Right", "Move Forward", "Move Backward");
			Velocity = inputVector * Speed * (float)delta;
			MoveAndSlide();
		}
	}
}

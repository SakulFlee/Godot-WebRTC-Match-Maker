using Godot;

public partial class Player : CharacterBody2D
{
	public override void _Process(double delta)
	{
		var inputDirection = Input.GetVector("left", "right", "up", "down");
		Velocity = inputDirection;
	}

	public override void _PhysicsProcess(double delta)
	{
		MoveAndSlide();
	}
}

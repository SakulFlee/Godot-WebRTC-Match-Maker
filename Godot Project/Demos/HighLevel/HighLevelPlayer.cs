using Godot;

public partial class HighLevelPlayer : CharacterBody2D
{
	[Export]
	public float SpeedMultiplier = 400.0f;

	public override void _PhysicsProcess(double delta)
	{
		Velocity = Input.GetVector("Move Left", "Move Right", "Move Forward", "Move Backward") * SpeedMultiplier;
		MoveAndSlide();
	}
}

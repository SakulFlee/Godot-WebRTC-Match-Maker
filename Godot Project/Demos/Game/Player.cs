using System;
using Godot;

public partial class Player : CharacterBody2D
{
	[Export]
	public string PeerUUID;

	[Export]
	public bool IsControlledByUs = false;

	[Export]
	public bool IsHost = false;

	[Export]
	public float Speed = 400f;

	[Export]
	public float InputTimerInterval = 0.01f;

	[Export]
	public float PositionTimerInterval = 0.001f;

	[Export]
	public float PositionDifferenceAllowed = 1.0f;

	private RichTextLabel idLabel;
	private Camera2D camera;

	private Vector2 previousInputVector = new();
	private Vector2 previousPosition = new();

	[Signal]
	public delegate void OnInputChangedEventHandler(Vector2 inputVector);

	[Signal]
	public delegate void OnPositionChangedEventHandler(string peerUUID, Vector2 position);

	public override void _Ready()
	{
		Name = $"Player#{PeerUUID}";

		idLabel = GetNode<RichTextLabel>("IDLabel");
		idLabel.Text = $"{{{PeerUUID}}}";

		camera = GetNode<Camera2D>("Camera2D");
		camera.Enabled = IsControlledByUs;

		setupInputTimer();

		if (IsHost)
		{
			setupPositionTimer();
		}
	}

	private void setupInputTimer()
	{
		var timer = new Timer()
		{
			Name = "Multiplayer P2P Input Vector Sync",
			WaitTime = InputTimerInterval,
			OneShot = false,
			Autostart = true,
		};
		timer.Timeout += () =>
		{
			var currentInputVector = Input.GetVector("Move Left", "Move Right", "Move Forward", "Move Backward");

			var xDiff = Math.Abs(previousInputVector.X - currentInputVector.X);
			var yDiff = Math.Abs(previousInputVector.Y - currentInputVector.Y);
			const float epsilon = 0.01f;

			if (xDiff >= epsilon || yDiff >= epsilon)
			{
				previousInputVector = currentInputVector;

				EmitSignal(SignalName.OnInputChanged, currentInputVector);

				if (IsControlledByUs)
				{
					ApplyInputVector(currentInputVector);
				}
			}
		};
		AddChild(timer);
	}

	private void setupPositionTimer()
	{
		var timer = new Timer()
		{
			Name = "Multiplayer H2C Position Sync",
			WaitTime = PositionTimerInterval,
			OneShot = false,
			Autostart = true,
		};
		timer.Timeout += () =>
		{
			var currentPosition = Position;

			var xDiff = Math.Abs(previousPosition.X - currentPosition.X);
			var yDiff = Math.Abs(previousPosition.Y - currentPosition.Y);
			const float epsilon = 0.01f;

			if (xDiff >= epsilon || yDiff >= epsilon)
			{
				previousPosition = currentPosition;

				EmitSignal(SignalName.OnPositionChanged, PeerUUID, currentPosition);
			}
		};
		AddChild(timer);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Velocity.X >= 0.1 || Velocity.X <= -0.1 || Velocity.Y >= 0.1 || Velocity.Y <= -0.1)
		{
			MoveAndSlide();
		}
	}

	public void ApplyInputVector(Vector2 inputVector)
	{
		Velocity = inputVector * Speed;
	}

	public void ApplyPosition(Vector2 positionCorrection)
	{
		var difference = Position - positionCorrection;

		var absX = Math.Abs(difference.X);
		var absY = Math.Abs(difference.Y);

		if (absX >= PositionDifferenceAllowed || absY >= PositionDifferenceAllowed)
		{
			// Only correct the position if the difference is too great.
			// Avoids rubber-banding while decreasing accuracy.
			Position = positionCorrection;
		}
	}
}

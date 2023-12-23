using System;
using Godot;

public partial class Player : CharacterBody2D
{
	#region Exports
	[Export]
	public string PeerUUID;

	[Export]
	public bool IsControlledByUs = false;

	[Export]
	public bool IsHost = false;

	[Export]
	public float Speed = 400f;
	#endregion

	#region Nodes
	private RichTextLabel idLabel;
	private Camera2D camera;
	#endregion

	#region Value trackers
	private Vector2 previousInputVector = new();
	private Vector2 previousPosition = new();
	#endregion

	#region Signals
	[Signal]
	public delegate void OnInputChangedEventHandler(Vector2 inputVector);

	[Signal]
	public delegate void OnPositionChangedEventHandler(string peerUUID, Vector2 position);
	#endregion

	public override void _EnterTree()
	{
		idLabel = GetNode<RichTextLabel>("IDLabel");
		camera = GetNode<Camera2D>("Camera2D");
	}

	public override void _Ready()
	{
		Name = $"Player#{PeerUUID}";
		idLabel.Text = $"{{{PeerUUID}}}";
		camera.Enabled = IsControlledByUs;
	}

	public override void _PhysicsProcess(double delta)
	{
		handleInputs();

		if (checkVectorThreshold(Velocity, 0.1f))
		{
			MoveAndSlide();
		}

		if (IsHost)
		{
			signalPositionChange();
		}
	}

	private void handleInputs()
	{
		var currentInputVector = Input.GetVector("Move Left", "Move Right", "Move Forward", "Move Backward");

		var xDiff = Math.Abs(previousInputVector.X - currentInputVector.X);
		var yDiff = Math.Abs(previousInputVector.Y - currentInputVector.Y);
		const float epsilon = 0.01f;

		if (xDiff >= epsilon || yDiff >= epsilon)
		{
			previousInputVector = currentInputVector;

			if (IsControlledByUs)
			{
				ApplyInputVector(currentInputVector);
			}

			EmitSignal(SignalName.OnInputChanged, currentInputVector);
		}
	}

	private void signalPositionChange()
	{
		if (checkVectorDifference(previousPosition, Position, 0.01f))
		{
			previousPosition = Position;
			EmitSignal(SignalName.OnPositionChanged, PeerUUID, Position);
		}
	}

	private bool checkVectorDifference(Vector2 a, Vector2 b, float epsilon)
	{
		var xDiff = Math.Abs(a.X - b.X);
		var yDiff = Math.Abs(a.Y - b.Y);

		return xDiff >= epsilon || yDiff >= epsilon;
	}

	private bool checkVectorThreshold(Vector2 v, float threshold)
	{
		var _v = v.Abs();
		var _t = Math.Abs(threshold);
		return _v.X >= _t || _v.Y >= _t;
	}

	public void ApplyInputVector(Vector2 inputVector)
	{
		Velocity = inputVector * Speed;
	}

	public void ApplyPosition(Vector2 positionCorrection)
	{
		Position = positionCorrection;
	}
}

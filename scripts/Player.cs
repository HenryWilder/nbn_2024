using Godot;

public partial class Player : CharacterBody3D
{
	private static readonly PackedScene impactGrenade = GD.Load<PackedScene>("res://Grenades/ImpactGrenade.tscn");

	[Export]
	private Node3D Hand { get; set; }

	public const float RUN_SPEED = 5.0f;
	public const float JUMP_SPEED = 4.5f;

	public override void _PhysicsProcess(double delta)
	{
		Velocity = CalculateVelocity(Velocity, delta);
		MoveAndSlide();

		if (Input.IsActionJustPressed("primary_fire"))
		{
			ThrowGrenade();
		}
	}

	Vector3 CalculateVelocity(Vector3 velocity, double dt)
	{
		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)dt;
		}
		// Handle Jump.
		else if (Input.IsActionJustPressed("movement_jump"))
		{
			velocity.Y = JUMP_SPEED;
		}

		// Get the input direction and handle the movement/deceleration.
		Vector2 inputDir = Input.GetVector(
			"movement_left",    "movement_right",
			"movement_forward", "movement_backward"
		);
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * RUN_SPEED;
			velocity.Z = direction.Z * RUN_SPEED;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, RUN_SPEED);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, RUN_SPEED);
		}

		return velocity;
	}

	void ThrowGrenade()
	{
		Node nade = impactGrenade.Instantiate();
		GetOwner().AddChild(nade);
		var rootNode = nade.GetNode<RigidBody3D>("/ImpactGrenade");
		GD.Print(rootNode);
		rootNode.GlobalPosition = Hand.GlobalPosition;
	}
}

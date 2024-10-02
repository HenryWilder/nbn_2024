using Godot;

public partial class GrenadeBase : Area3D
{
	public override void _Ready()
	{
	}

	public override void _Process(double delta)
	{
	}

	public virtual void Detonate()
	{
		// todo
		QueueFree();
	}
}

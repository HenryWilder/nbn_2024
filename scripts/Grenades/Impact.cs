using Godot;

public partial class Impact : GrenadeBase
{
	public override void _Ready()
	{
	}

	public override void _Process(double delta)
	{
	}

    protected override void OnHit(Node other)
    {
		Explode();
    }
}

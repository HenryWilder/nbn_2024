using Godot;

public partial class Sticky : GrenadeBase
{
    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
    }

    protected override void OnHit(Node other)
    {
        // todo: stick to the other object
    }

    public void RemoteTrigger()
    {
        Explode();
    }
}

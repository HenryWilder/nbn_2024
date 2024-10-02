using Godot;

/// <summary>
/// The class that all grenades derive from.
/// Includes methods for shared grenade functionality like hit detection and exploding.
/// </summary>
public partial class GrenadeBase : RigidBody3D
{
    public override void _Ready()
    {
        ContactMonitor = true;
        MaxContactsReported = 10;
        BodyEntered += OnHit;
    }

    public override void _Process(double delta)
    {
    }

    protected virtual void OnHit(Node other)
    {
        if (other is PhysicsBody3D body) {
            OnHitBody(body);
        } else if (other is GridMap grid) {
            OnHitGrid(grid);
        } else {
            // the documentation says the collision must be one of those two.
            // if it is not, I would like to know what it is.
            GD.PrintErr($"hit node ({other}) is neither a PhysicsBody3D nor GridMap");
        }
    }

    protected virtual void OnHitBody(PhysicsBody3D body)
    {
        // do nothing
    }

    protected virtual void OnHitGrid(GridMap grid)
    {
        // do nothing
    }

    protected virtual void Explode()
    {
        // todo: explosion effect
        QueueFree();
    }
}

using Godot;

/// <summary>
/// The class that all grenades derive from.
/// Includes methods for shared grenade functionality like hit detection and exploding.
/// </summary>
public partial class GrenadeBase : RigidBody3D
{
    private bool isEnabled;
    public bool IsEnabled
    {
        get => isEnabled;
        set {
            bool isChanged = value != isEnabled;
            isEnabled = value;
            if (isChanged) {
                if (isEnabled) {
                    GD.Print("Activated");
                    EmitSignal(SignalName.GrenadeEnabled);
                } else {
                    GD.Print("Deactivated");
                    EmitSignal(SignalName.GrenadeDisabled);
                }
            }
        }
    }

    [Signal]
    public delegate void GrenadeDisabledEventHandler();

    [Signal]
    public delegate void GrenadeEnabledEventHandler();

    [Signal]
    public delegate void GrenadeDetonatedEventHandler();

    public override void _Ready()
    {
        IsEnabled = true;
        ContactMonitor = true;
        MaxContactsReported = 10;
        BodyEntered += OnHit;
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
        // optional override
    }

    protected virtual void OnHitGrid(GridMap grid)
    {
        // optional override
    }

    protected virtual void Explode()
    {
        GD.Print("Boom!");
        // todo: explosion effect
        EmitSignal(SignalName.GrenadeDetonated);
        QueueFree();
    }
}

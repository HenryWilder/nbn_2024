using Godot;

public partial class Customizer : GrenadeBase
{
    private readonly Device device = new();
    private ulong startTime = ulong.MaxValue;

    public override void _Ready()
    {
        startTime = Time.GetTicksMsec();
    }

    public override void _Process(double delta)
    {
        device.sensors.tms = (ushort)(Time.GetTicksMsec() - startTime);
        device.cpu.Step();
    }

    public override void _PhysicsProcess(double delta)
    {
        const float MILLI = 1.0f / 1000.0f;
        base._PhysicsProcess(delta);
        device.sensors.vel = (ushort)(LinearVelocity.Length() * MILLI);
    }

    protected override void OnHit(Node other)
    {
        device.sensors.hit = 1; // todo: think of a clever use for the other 15 bits
    }

    public ROM Insert(ROM rom)
    {
        (var old, device.rom) = (device.rom, rom);
        return old;
    }
}

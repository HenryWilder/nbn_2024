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
        device.Time = (short)(Time.GetTicksMsec() - startTime);
        device.Step();
    }

    public void InsertROM(ROM rom)
    {
        device.InsertROM(rom);
    }
}

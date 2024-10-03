using System;
using Godot;

public partial class Customizer : GrenadeBase
{
    private readonly Device device = new();
    private ulong startTime = Time.GetTicksMsec();

    public override void _Ready()
    {
        // todo: have user insert a ROM manually
        Insert(ROM.ExampleROM);
    }

    public override void _Process(double delta)
    {
        device.cpu.reg.Tms = (short)(Time.GetTicksMsec() - startTime);
        const float MILLI = 1.0f / 1000.0f;
        device.cpu.reg.Vel = (short)(LinearVelocity.Length() * MILLI);
        try {
            device.Step();
        } catch (Exception e) {
            GD.PrintErr($"Error: {e}\nInitiating self-destruct.");
            device.cpu.reg.Bam = 1;
        }
        if (device.cpu.reg.Bam != 0) {
            Explode();
        }
    }

    protected override void OnHit(Node other)
    {
        device.cpu.reg.Hit = 1; // todo: think of a clever use for the other 15 bits
    }

    public ROM Insert(ROM rom)
    {
        GD.Print($"Inserting ROM:\n```\n{rom}\n```");
        (var old, device.rom) = (device.rom, rom);
        return old;
    }
}

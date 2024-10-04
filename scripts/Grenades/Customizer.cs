using System;
using Godot;

public partial class Customizer : GrenadeBase
{
    public readonly struct ROMInsertion
    {
        public readonly string kind;
        public readonly string programString;
    }

    private readonly Device device = new();
    private ulong startTime = Time.GetTicksMsec();

    [Export]
    public PhysicalRom PhysicalRom;

    public override void _Ready()
    {
        var (prgm, initRam) = PhysicalRom.Compile();
        if (initRam is not null) device.ram.Write(initRam);
        Insert(prgm);
    }

    public override void _Process(double delta)
    {
        if (IsEnabled) {
            device.cpu.reg.Tms = (short)(Time.GetTicksMsec() - startTime);
            const float MILLI = 1.0f / 1000.0f;
            device.cpu.reg.Vel = (short)(LinearVelocity.Length() * MILLI);
            try {
                device.Step();
            } catch (Exception e) {
                GD.PrintErr($"Emulator error: {e.Message}\nInitiating self-destruct");
                device.cpu.reg.Bam = 1;
            }
            if (device.cpu.reg.Bam != 0) {
                Explode();
            }
        }
    }

    protected override void OnHit(Node other)
    {
        device.cpu.reg.Hit = 1; // todo: think of a clever use for the other 15 bits
    }

    public void Insert(ROM rom)
    {
        device.rom = rom;
        if (device.IsRomInserted()) {
            GD.PrintRich($"Inserting ROM:\n```\n{rom.ToRich()}\n```");
            IsEnabled = true;
        } else {
            GD.PrintErr("No ROM is inserted.");
            IsEnabled = false;
        }
    }
}

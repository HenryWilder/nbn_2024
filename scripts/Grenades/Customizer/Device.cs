// Customizer grenade CPU registers are 16-bit

public class Device
{
    public Device()
    {
        cpu = new CPU(this);
    }

    private readonly CPU cpu;
    public readonly RAM ram = new();
    private ROM rom = null;

    public ROM SwapROM(ROM rom)
    {
        (rom, this.rom) = (this.rom, rom);
        return rom;
    }

    public void InsertROM(ROM rom) => SwapROM(rom);

    public ROM EdjectROM() => SwapROM(null);

    public short Time      { get => cpu.Tms; set => cpu.Tms = value; }
    public short Speed     { get => cpu.Vel; set => cpu.Vel = value; }
    public short Impact    { get => cpu.Hit; set => cpu.Hit = value; }
    public short Proximity { get => cpu.Prx; set => cpu.Prx = value; }
    public short Bam       { get => cpu.Bam; set => cpu.Bam = value; }

    public bool IsRomInserted()
    {
        return rom is not null;
    }

    public Line Line(ushort n)
    {
        return rom.GetLine(n);
    }

    public void Step()
    {
        cpu.Step();
    }
}

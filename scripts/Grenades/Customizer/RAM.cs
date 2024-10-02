using System;

public class RAM
{
    /// <summary>
    /// Memory
    /// </summary>
    private readonly byte[] ram = new byte[512];

    public byte LoadByte(ushort index)
    {
        return ram[index];
    }

    public void SaveByte(ushort index, byte value)
    {
        ram[index] = value;
    }

    public short LoadWord(ushort index)
    {
        const int WIDTH = sizeof(short);
        int start = index * WIDTH;
        int end = start + WIDTH;
        return BitConverter.ToInt16(ram.AsSpan(start..end));
    }

    public void SaveWord(ushort index, ushort value)
    {
        const int WIDTH = sizeof(short);
        int start = index * WIDTH;
        byte[] bytes = BitConverter.GetBytes(value);
        for (int i = 0; i < sizeof(ushort); ++i) {
            ram[start + i] = bytes[i];
        }
    }
}

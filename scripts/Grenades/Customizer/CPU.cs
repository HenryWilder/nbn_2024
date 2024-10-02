using System;

public class CPU
{
    public CPU(Device device)
    {
        this.device = device;
    }

    /// <summary>
    /// The device this CPU is contained in
    /// </summary>
    private readonly Device device;

    /// <summary>
    /// Current line number
    /// </summary>
    private ushort counter = 0;

    public enum Register {
        R0,
        R1,
        R2,
        R3,
        /// <summary>
        /// Time since thrown (ms)
        /// </summary>
        TMS,
        /// <summary>
        /// Speed (mm/s)
        /// </summary>
        VEL,
        /// <summary>
        /// Impact
        /// </summary>
        HIT,
        /// <summary>
        /// Proximity
        /// </summary>
        PRX,
        /// <summary>
        /// Detonate
        /// </summary>
        BAM,
    }

    /// <summary>
    /// General-purpose registers
    /// </summary>
    private readonly short[] reg = new short[Enum.GetNames(typeof(Register)).Length];

    public short Tms { get => reg[(int)Register.TMS]; set => reg[(int)Register.TMS] = value; }
    public short Vel { get => reg[(int)Register.VEL]; set => reg[(int)Register.VEL] = value; }
    public short Hit { get => reg[(int)Register.HIT]; set => reg[(int)Register.HIT] = value; }
    public short Prx { get => reg[(int)Register.PRX]; set => reg[(int)Register.PRX] = value; }
    public short Bam { get => reg[(int)Register.BAM]; set => reg[(int)Register.BAM] = value; }

    private readonly CPUFlags flags = new();

    /// <summary>
    /// Associates register names with the arg value needed to access that register
    /// </summary>
    /// <param name="name"> The name of the register </param>
    /// <returns> -1 on error </returns>
    public static int RegisterIndex(string name)
        => name switch {
            "r0"  => (int)Register.R0,
            "r1"  => (int)Register.R1,
            "r2"  => (int)Register.R2,
            "r3"  => (int)Register.R3,
            "tms" => (int)Register.TMS,
            "vel" => (int)Register.VEL,
            "hit" => (int)Register.HIT,
            "prx" => (int)Register.PRX,
            "bam" => (int)Register.BAM,
            _ => -1,
        };

    private short Get(bool isImm, short arg)
    {
        if (isImm) {
            return arg;
        } else {
            return reg[arg];
        }
    }

    private static ushort ToUInt16(short value)
        => BitConverter.ToUInt16(BitConverter.GetBytes(value));

    public void Step()
    {
        Line line = device.Line(counter);
        var (op, isJImm, is1Imm, is2Imm) = line.opcode.Split();
        short arg1 = Get(is1Imm, line.arg1);
        short arg2 = Get(is2Imm, line.arg2);
        bool isJumping = false;
        switch (op) {
            case Instruction.NOP:                             break;
            case Instruction.MOV: reg[line.roja] = arg1;      break;
            case Instruction.JMP: isJumping = true;           break;
            case Instruction.JE:  isJumping = arg1 == arg2;   break;
            case Instruction.JNE: isJumping = arg1 != arg2;   break;
            case Instruction.JZ:  isJumping = arg1 == 0;      break;
            case Instruction.JNZ: isJumping = arg1 != 0;      break;
            case Instruction.JG:  isJumping = arg1 > arg2;    break;
            case Instruction.JL:  isJumping = arg1 < arg2;    break;
            case Instruction.JGE: isJumping = arg1 >= arg2;   break;
            case Instruction.JLE: isJumping = arg1 <= arg2;   break;
            case Instruction.JS:  isJumping = flags.StatSign; break;
        }
        if (!isJumping) {
            ++counter;
        } else {
            counter = ToUInt16(line.roja);
        }
    }
}

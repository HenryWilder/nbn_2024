using System.Linq;

public struct Line
{
    public Opcode opcode;
    /// <summary>
    /// Result or jump address
    /// </summary>
    public short roja;
    public short arg1;
    public short arg2;

    public Line(in Opcode opcode, short roja, short arg1, short arg2)
    {
        this.opcode = opcode;
        this.roja = roja;
        this.arg1 = arg1;
        this.arg2 = arg2;
    }
}

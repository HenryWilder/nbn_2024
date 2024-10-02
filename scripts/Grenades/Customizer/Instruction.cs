
using System.Collections.Generic;

public enum Instruction : ushort
{
    /// <summary>
    /// No operation
    /// </summary>
    NOP,

    /// <summary>
    /// Set
    /// </summary>
    MOV,

    /// <summary>
    /// Jump to a specific line
    /// </summary>
    JMP,

    /// <summary>
    /// Jump if equal
    /// </summary>
    JE,

    /// <summary>
    /// Jump if not equal
    /// </summary>
    JNE,

    /// <summary>
    /// Jump if zero
    /// </summary>
    JZ,

    /// <summary>
    /// Jump if not zero
    /// </summary>
    JNZ,

    /// <summary>
    /// Jump if greater
    /// </summary>
    JG,

    /// <summary>
    /// Jump if less
    /// </summary>
    JL,

    /// <summary>
    /// Jump if greater or equal
    /// </summary>
    JGE,

    /// <summary>
    /// Jump if less or equal
    /// </summary>
    JLE,

    /// <summary>
    /// Jump if sign bit is set
    /// </summary>
    JS,
}

public static class InstructionExtension
{
    private static readonly Dictionary<string, Instruction> mapping = new()
    {
        { "nop", Instruction.NOP },
        { "mov", Instruction.MOV },
        { "jmp", Instruction.JMP },
        { "je",  Instruction.JE  },
        { "jne", Instruction.JNE },
        { "jz",  Instruction.JZ  },
        { "jnz", Instruction.JNZ },
        { "jg",  Instruction.JG  },
        { "jl",  Instruction.JL  },
        { "jge", Instruction.JGE },
        { "jle", Instruction.JLE },
        { "js",  Instruction.JS  },
    };

    public static bool TryIntoInstruction(this string str, out Instruction value)
        => mapping.TryGetValue(str, out value);
}

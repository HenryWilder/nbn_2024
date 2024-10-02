using System;

public readonly struct Opcode
{
    const int ARG0_IS_IMMEDIATE_BIT = 0b1000000000000000;
    const int ARG1_IS_IMMEDIATE_BIT = 0b0100000000000000;
    const int ARG2_IS_IMMEDIATE_BIT = 0b0010000000000000;
    const int INSTRUCTION_BITMASK   = 0b0001111111111111;

    private readonly short opcode;

    public Opcode(Instruction op, bool isJImm, bool is1Imm, bool is2Imm)
    {
        opcode = (short)(
            (int)op
            | (isJImm ? ARG0_IS_IMMEDIATE_BIT : 0)
            | (is1Imm ? ARG1_IS_IMMEDIATE_BIT : 0)
            | (is2Imm ? ARG2_IS_IMMEDIATE_BIT : 0)
        );
    }

    public (Instruction op, bool isJImm, bool is1Imm, bool is2Imm) Split()
    {
        Instruction op = (Instruction)(opcode & INSTRUCTION_BITMASK);
        bool isJImm = (opcode & ARG0_IS_IMMEDIATE_BIT) != 0;
        bool is1Imm = (opcode & ARG1_IS_IMMEDIATE_BIT) != 0;
        bool is2Imm = (opcode & ARG2_IS_IMMEDIATE_BIT) != 0;
        return (op, isJImm, is1Imm, is2Imm);
    }
}

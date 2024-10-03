using System;
using System.Runtime.InteropServices;
using Godot;
using static Device.CPU.Instruction;

public class Device
{
    public Device() {}

    #region CPU
    public struct CPU
    {
        public CPU() {}

        #region Instruction
        public enum Instruction : byte
        {
            /// <summary> No operation </summary>
            NOP,
            /// <summary> Set </summary>
            MOV,
            /// <summary> Add </summary>
            ADD,
            /// <summary> Subtract </summary>
            SUB,
            /// <summary> Multipy </summary>
            MUL,
            /// <summary> Divide </summary>
            DIV,
            /// <summary> Bitwise AND </summary>
            AND,
            /// <summary> Bitwise OR </summary>
            ORR,
            /// <summary> Bitwise NOT </summary>
            NOT,
            /// <summary> Bitwise XOR </summary>
            XOR,
            /// <summary> Jump to a specific line </summary>
            JMP,
            /// <summary> Jump if equal </summary>
            JE,
            /// <summary> Jump if not equal </summary>
            JNE,
            /// <summary> Jump if zero </summary>
            JZ,
            /// <summary> Jump if not zero </summary>
            JNZ,
            /// <summary> Jump if greater </summary>
            JG,
            /// <summary> Jump if less </summary>
            JL,
            /// <summary> Jump if greater or equal </summary>
            JGE,
            /// <summary> Jump if less or equal </summary>
            JLE,
            /// <summary> Jump if sign bit is set </summary>
            JS,
        }

        public static bool IsMath(Instruction op)
            => op is ADD or SUB or MUL or DIV or AND or ORR or NOT or XOR or MOV;

        public static bool IsJump(Instruction op)
            => op is JMP or JE or JNE or JZ or JNZ or JG or JL or JGE or JLE or JS;
        #endregion

        #region Opcode
        [StructLayout(LayoutKind.Explicit, Pack = sizeof(short))]
        public readonly struct Opcode
        {
            private const ushort ARG1_IS_IMMEDIATE_BIT
                = 0b1000000000000000;
            private const ushort ARG2_IS_IMMEDIATE_BIT
                = 0b0100000000000000;
            private const ushort INSTRUCTION_BITMASK
                = 0b0011111111111111;

            public Opcode(Instruction op, bool is1Imm, bool is2Imm)
            {
                data = (ushort)(
                    (is1Imm ? ARG1_IS_IMMEDIATE_BIT : 0) |
                    (is2Imm ? ARG2_IS_IMMEDIATE_BIT : 0) |
                    (int)op
                );
            }

            [FieldOffset(0)]
            private readonly ushort data;

            public readonly Instruction Operation => (Instruction)(data & INSTRUCTION_BITMASK);
            public readonly bool IsArg1Immediate => (data & ARG1_IS_IMMEDIATE_BIT) != 0;
            public readonly bool IsArg2Immediate => (data & ARG2_IS_IMMEDIATE_BIT) != 0;
        }
        #endregion

        #region Registers
        public unsafe struct Registers
        {
            public Registers() { }

            private const int NUM_GP_REGISTERS = 4;
            private const int TMS_INDEX = NUM_GP_REGISTERS + 0;
            private const int VEL_INDEX = NUM_GP_REGISTERS + 1;
            private const int HIT_INDEX = NUM_GP_REGISTERS + 2;
            private const int PRX_INDEX = NUM_GP_REGISTERS + 3;
            private const int BAM_INDEX = NUM_GP_REGISTERS + 4;
            private const int NUM_SPEC_REGISTERS = 5;

            private fixed short data[NUM_GP_REGISTERS + NUM_SPEC_REGISTERS];

            #region Specialized Registers

            /// <summary> Time since thrown (milliseconds) </summary>
            public short Tms { readonly get => data[TMS_INDEX]; set => data[TMS_INDEX] = value; }

            /// <summary> Speed (milliunits/second) </summary>
            public short Vel { readonly get => data[VEL_INDEX]; set => data[VEL_INDEX] = value; }

            /// <summary> Impact </summary>
            public short Hit { readonly get => data[HIT_INDEX]; set => data[HIT_INDEX] = value; }

            /// <summary> Proximity </summary>
            public short Prx { readonly get => data[PRX_INDEX]; set => data[PRX_INDEX] = value; }

            /// <summary> Detonate </summary>
            public short Bam { readonly get => data[BAM_INDEX]; set => data[BAM_INDEX] = value; }

            #endregion

            public readonly short this[bool isImmediate, short index] {
                get => isImmediate ? index : data[index];
            }

            // it is illegal to assign to immediate
            public short this[short index] {
                set => data[index] = value;
            }

            /// <summary>
            /// Associates register names with the arg value needed to access that register
            /// </summary>
            public static short Lookup(string name)
            {
                switch (name) {
                    case "r0": return 0;
                    case "r1": return 1;
                    case "r2": return 2;
                    case "r3": return 3;
                    case "tms": return TMS_INDEX;
                    case "vel": return VEL_INDEX;
                    case "hit": return HIT_INDEX;
                    case "prx": return PRX_INDEX;
                    case "bam": return BAM_INDEX;

                    default:
                        GD.PrintErr($"\"{name}\" is not a valid register address");
                        return -69;
                }
            }
        }
        /// <summary>
        /// General-purpose registers
        /// </summary>
        public Registers reg = new();
        #endregion

        #region Status Register
        private struct ProgramStatus
        {
            public ProgramStatus() {}

            private const uint MODE_MASK
                = 0b00000000000000000000000000000011;
            private const uint PC_MASK
                = 0b00000011111111111111111111111100;
            private const uint FAST_INTURRUPT_BIT
                = 0b00000100000000000000000000000000;
            private const uint NORMAL_INTURRUPT_BIT
                = 0b00001000000000000000000000000000;
            private const uint OVERFLOW_BIT
                = 0b00010000000000000000000000000000;
            private const uint CARRY_BIT
                = 0b00100000000000000000000000000000;
            private const uint ZERO_BIT
                = 0b01000000000000000000000000000000;
            private const uint SIGN_BIT
                = 0b10000000000000000000000000000000;
            private const uint ARITHMETIC_MASK
                = OVERFLOW_BIT | CARRY_BIT | ZERO_BIT | SIGN_BIT;

            private uint data = 0;

            public uint Mode {
                readonly get => data & MODE_MASK;
                set => data = (data & ~MODE_MASK) | (value & MODE_MASK);
            }

            /// <summary>
            /// Program Counter
            /// </summary>
            public int PC {
                readonly get => (int)((data & PC_MASK) >> 2);
                set => data = (data & ~PC_MASK) | (((uint)value << 2) & PC_MASK);
            }

            public bool FastInturruptBit {
                readonly get => (data & FAST_INTURRUPT_BIT) != 0;
                set => data = value ? data | FAST_INTURRUPT_BIT : data & ~FAST_INTURRUPT_BIT;
            }

            public bool NormalInturruptBit {
                readonly get => (data & NORMAL_INTURRUPT_BIT) != 0;
                set => data = value ? data | NORMAL_INTURRUPT_BIT : data & ~NORMAL_INTURRUPT_BIT;
            }

            /// <summary> Signed and doesn't fit in register </summary>
            public bool OverflowBit {
                readonly get => (data & OVERFLOW_BIT) != 0;
                set => data = value ? data | OVERFLOW_BIT : data & ~OVERFLOW_BIT;
            }

            /// <summary> Unsigned and doesn't fit in register </summary>
            public bool CarryBit {
                readonly get => (data & CARRY_BIT) != 0;
                set => data = value ? data | CARRY_BIT : data & ~CARRY_BIT;
            }

            /// <summary> Zero result </summary>
            public bool ZeroBit {
                readonly get => (data & ZERO_BIT) != 0;
                set => data = value ? data | ZERO_BIT : data & ~ZERO_BIT;
            }

            /// <summary> Negative result </summary>
            public bool SignBit {
                readonly get => (data & SIGN_BIT) != 0;
                set => data = value ? data | SIGN_BIT : data & ~SIGN_BIT;
            }

            public void SetArithmeticFlags(int result) {
                data = (data & ~ARITHMETIC_MASK)
                    | (result > short.MaxValue ? OVERFLOW_BIT : 0)
                    | (result > ushort.MaxValue ? CARRY_BIT : 0)
                    | (result == 0 ? ZERO_BIT : 0)
                    | (result < 0 ? SIGN_BIT : 0);
            }
        }
        private ProgramStatus status = new();
        public int CurrentLine => status.PC;
        #endregion

        #region Stack
        /// <summary> Stack pointer </summary>
        private int SP = 0; // todo
        #endregion

        #region Step
        public void Execute(ROM.Line line)
        {
            GD.Print($"{reg.Tms}ms: Running line {status.PC}: \"{line}\"");
            short arg1 = reg[line.opcode.IsArg1Immediate, line.arg1];
            short arg2 = reg[line.opcode.IsArg2Immediate, line.arg2];
            var op = line.opcode.Operation;

            int result = 0;
            bool isJumping = false;
            switch (op) {
                #region Math

                case MOV: result = arg1; break;

                // arithmetic
                case ADD: result = arg1 + arg2; break;
                case SUB: result = arg1 - arg2; break;
                case MUL: result = arg1 * arg2; break;
                case DIV: result = arg1 / arg2; break;

                // logic
                case AND: result = arg1 & arg2; break;
                case ORR: result = arg1 | arg2; break;
                case NOT: result = ~arg1;       break;
                case XOR: result = arg1 ^ arg2; break;

                #endregion

                #region Jump

                case JMP: isJumping = true;            break;
                case JE:  isJumping = arg1 == arg2;    break;
                case JNE: isJumping = arg1 != arg2;    break;
                case JZ:  isJumping = status.ZeroBit;  break;
                case JNZ: isJumping = !status.ZeroBit; break;
                case JG:  isJumping = arg1 > arg2;     break;
                case JL:  isJumping = arg1 < arg2;     break;
                case JGE: isJumping = arg1 >= arg2;    break;
                case JLE: isJumping = arg1 <= arg2;    break;
                case JS:  isJumping = status.SignBit;  break;

                #endregion

                #region Other

                case NOP: /* do nothing */ break;

                #endregion
            };

            // ALU
            if (IsMath(op)) {
                status.SetArithmeticFlags(result);
                reg[line.Result] = (short)result;
                GD.Print(
                    $"  Assigning {(short)result} to register {line.Result}\n" +
                    $"    Status flags updated: Overflow={status.OverflowBit}, Carry={status.CarryBit}, Zero={status.ZeroBit}, Sign={status.SignBit}"
                );
            }

            // Update PC
            if (isJumping) {
                GD.Print($"  Jumping from line {status.PC} to line {line.JumpAddress}");
                status.PC = line.JumpAddress;
            } else {
                ++status.PC;
                GD.Print($"  Stepping to line {status.PC}");
            }
        }
        #endregion
    }
    public CPU cpu = new();
    #endregion

    #region RAM
    public unsafe struct RAM
    {
        public const int RAM_SIZE = 512;

        public RAM() {}

        private fixed byte memory[RAM_SIZE];

        private static void GuardBytes<T>(int index, int numElements = 1) where T: unmanaged
        {
            if ((index + numElements) * sizeof(T) > RAM_SIZE) {
                throw new IndexOutOfRangeException();
            }
        }

        public readonly T Load<T>(int index) where T: unmanaged
        {
            T result;
            GuardBytes<T>(index);
            fixed (byte* bytePtr = memory) {
                T* tPtr = (T*)bytePtr;
                result = tPtr[index];
            }
            return result;
        }

        public void Save<T>(int index, T value) where T: unmanaged
        {
            GuardBytes<T>(index);
            fixed (byte* bytePtr = memory) {
                T* tPtr = (T*)bytePtr;
                tPtr[index] = value;
            }
        }

        public void Write<T>(int index, T[] data) where T: unmanaged
        {
            GuardBytes<T>(index, data.Length);
            fixed (byte* bytePtr = memory) {
                T* tPtr = (T*)bytePtr;
                foreach (T item in data) {
                    tPtr[index++] = item;
                }
            }
        }
    }
    public RAM ram = new();
    #endregion

    #region ROM
    public ROM rom = null;

    public void Step() {
        cpu.Execute(rom[cpu.CurrentLine]);
    }

    public bool IsRomInserted()
    {
        return rom is not null;
    }
    #endregion
}

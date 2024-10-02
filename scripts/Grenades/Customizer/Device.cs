using System;
using System.Runtime.InteropServices;

public class Device
{
    public Device()
    {
        cpu = new CPU(this);
    }

    #region CPU
    public struct CPU
    {
        public CPU(Device owner)
        {
            device = owner;
        }

        /// <summary>
        /// The device this CPU is contained in
        /// </summary>
        private readonly Device device;

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
                    (int)op
                    | (is1Imm ? ARG1_IS_IMMEDIATE_BIT : 0)
                    | (is2Imm ? ARG2_IS_IMMEDIATE_BIT : 0)
                );
            }

            [FieldOffset(0)]
            public readonly ushort data;

            public readonly Instruction Operation => (Instruction)(data & INSTRUCTION_BITMASK);
            public readonly bool IsArg1Immediate => (data & ARG1_IS_IMMEDIATE_BIT) != 0;
            public readonly bool IsArg2Immediate => (data & ARG2_IS_IMMEDIATE_BIT) != 0;
        }
        #endregion

        private unsafe struct Registers {
            private fixed short registers[4];

            public readonly short this[bool isImmediate, short index]
            {
                get => isImmediate ? index : registers[index];
            }

            // it is illegal to set immediate
            public short this[short index]
            {
                set => registers[index] = value;
            }
        }
        /// <summary>
        /// General-purpose registers
        /// </summary>
        private Registers reg = new();

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
                = 0b11110000000000000000000000000000;

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
        #endregion

        #region Stack
        /// <summary> Stack pointer </summary>
        private int SP = 0; // todo
        #endregion

        /// <summary>
        /// Associates register names with the arg value needed to access that register
        /// </summary>
        public static int RegisterIndex(string name)
            => name switch {
                "r0" => 0,
                "r1" => 1,
                "r2" => 2,
                "r3" => 3,
                _ => -1,
            };

        #region Step
        public void Step()
        {
            ROM.Line line = device.rom[status.PC];
            short arg1 = reg[line.opcode.IsArg1Immediate, line.arg1];
            short arg2 = reg[line.opcode.IsArg2Immediate, line.arg2];
            var op = line.opcode.Operation;
            {
                int result;
                switch (op) {
                    // arithmetic
                    case Instruction.ADD: result = arg1 + arg2; break;
                    case Instruction.SUB: result = arg1 - arg2; break;
                    case Instruction.MUL: result = arg1 * arg2; break;
                    case Instruction.DIV: result = arg1 / arg2; break;

                    // logic
                    case Instruction.AND: result = arg1 & arg2; break;
                    case Instruction.ORR: result = arg1 | arg2; break;
                    case Instruction.NOT: result = ~arg1;       break;
                    case Instruction.XOR: result = arg1 ^ arg2; break;
                    
                    // other
                    case Instruction.MOV: result = arg1; break;
                    default: goto OtherInstructions;
                };
                status.SetArithmeticFlags(result);
                // todo: does this account for the difference between int signbit and short signbit?
                reg[line.roja] = (short)result;
                goto UpdateCounter;
            }

        OtherInstructions:
            switch (op) {
                case Instruction.NOP:
                    // do nothing
                    break;
            }

        UpdateCounter:
            bool isJumping = op switch {
                Instruction.JMP => true,
                Instruction.JE  => arg1 == arg2,
                Instruction.JNE => arg1 != arg2,
                Instruction.JZ  => status.ZeroBit,
                Instruction.JNZ => !status.ZeroBit,
                Instruction.JG  => arg1 > arg2,
                Instruction.JL  => arg1 < arg2,
                Instruction.JGE => arg1 >= arg2,
                Instruction.JLE => arg1 <= arg2,
                Instruction.JS  => status.SignBit,
                _ => false
            };
            status.PC = !isJumping ? status.PC + 1 : line.roja;
        }
        #endregion
    }
    public readonly CPU cpu; // assigned in ctor because CPU needs access to `this`
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

    #region Sensor Array
    public struct SensorArray
    {
        /// <summary> Time since thrown (milliseconds) </summary>
        public ushort tms;

        /// <summary> Speed (milliunits/second) </summary>
        public ushort vel;

        /// <summary> Impact </summary>
        public short hit;

        /// <summary> Proximity </summary>
        public short prx;

        /// <summary> Detonate </summary>
        public bool bam;
    }
    public SensorArray sensors = new();
    #endregion

    #region ROM
    public ROM rom = null;

    public bool IsRomInserted()
    {
        return rom is not null;
    }
    #endregion
}

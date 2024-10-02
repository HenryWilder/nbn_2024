// Customizer grenade CPU registers are 16-bit

using System;

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
        #endregion
        
        #region Opcode
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
                var op = (Instruction)(opcode & INSTRUCTION_BITMASK);
                var isJImm = (opcode & ARG0_IS_IMMEDIATE_BIT) != 0;
                var is1Imm = (opcode & ARG1_IS_IMMEDIATE_BIT) != 0;
                var is2Imm = (opcode & ARG2_IS_IMMEDIATE_BIT) != 0;
                return (op, isJImm, is1Imm, is2Imm);
            }
        }
        #endregion

        /// <summary>
        /// Current line number
        /// </summary>
        private ushort counter = 0;

        /// <summary>
        /// General-purpose registers
        /// </summary>
        private readonly short[] reg = new short[4];

        #region Flags
        public struct CPUFlags
        {
            [Flags]
            public enum Flags : ushort {
                StatusCarry
                    = 0b000000001,
                StatusParity
                    = 0b000000010,
                StatusAuxCarry
                    = 0b000000100,
                StatusZero
                    = 0b000001000,
                StatusSign
                    = 0b000010000,
                StatusOverflow
                    = 0b000100000,
                ControlInterruptEnabled
                    = 0b001000000,
                ControlDirection
                    = 0b010000000,
                ControlTrap
                    = 0b100000000,
            }

            private Flags flags;

            public readonly bool GetFlag(Flags flag) => flags.HasFlag(flag);
            public void SetFlag(Flags flag, bool value) => flags = value ? flags | flag : flags & ~flag;

            public bool StatCarry
            { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
            public bool StatParity
            { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
            public bool StatAuxCarry
            { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
            public bool StatZero
            { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
            public bool StatSign
            { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
            public bool StatOverflow
            { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
            public bool CtrlInterruptEnabled
            { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
            public bool CtrlDirection
            { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
            public bool CtrlTrap
            { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
        }
        private readonly CPUFlags flags = new();
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

        private readonly short Get(bool isImmediate, short arg) => isImmediate ? arg : reg[arg];

        private static ushort ToUInt16(short value)
            => BitConverter.ToUInt16(BitConverter.GetBytes(value));

        #region Step
        public void Step()
        {
            ROM.Line line = device.rom.GetLine(counter);
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
        #endregion
    }
    private readonly CPU cpu;
    #endregion

    #region RAM
    public unsafe readonly struct RAM
    {
        public RAM() {}

        private readonly byte[] memory = new byte[512];

        private void GuardBytes<T>(int index, int numElements = 1) where T: unmanaged
        {
            if ((index + numElements) * sizeof(T) > memory.Length) {
                throw new IndexOutOfRangeException();
            }
        }

        public readonly byte LoadByte(int index)
        {
            return memory[index];
        }

        public void SaveByte(int index, byte value)
        {
            memory[index] = value;
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
    public readonly RAM ram = new();
    #endregion

    #region Sensor Array
    public struct SensorArray
    {
        /// <summary>
        /// Time since thrown (milliseconds)
        /// </summary>
        public ushort tms;

        /// <summary>
        /// Speed (milliunits/second)
        /// </summary>
        public ushort vel;

        /// <summary>
        /// Impact
        /// </summary>
        public short hit;

        /// <summary>
        /// Proximity
        /// </summary>
        public short prx;

        /// <summary>
        /// Detonate
        /// </summary>
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

    public void Step()
    {
        cpu.Step();
    }
}

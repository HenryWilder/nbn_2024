using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using static Device.CPU;

/// <summary>
/// Program memory
/// </summary>
public class ROM
{
    #region Line
    [StructLayout(LayoutKind.Explicit, Pack = sizeof(short))]
    public readonly struct Line
    {
        [FieldOffset(sizeof(short) * 0)]
        public readonly Opcode opcode;

        /// <summary> Result or jump address </summary>
        [FieldOffset(sizeof(short) * 1)]
        public readonly short roja;

        [FieldOffset(sizeof(short) * 2)]
        public readonly short arg1;

        [FieldOffset(sizeof(short) * 3)]
        public readonly short arg2;

        public Line(Opcode opcode, short roja, short arg1, short arg2)
        {
            this.opcode = opcode;
            this.roja = roja;
            this.arg1 = arg1;
            this.arg2 = arg2;
        }
    }
    #endregion

    #region Packed ROM
    private unsafe struct PackedROM
    {
        public const int ROM_SIZE = 256;

        public PackedROM() {}
        
        private readonly int numLines = 0; // May be fewer than the allocated space
        private unsafe fixed ulong data[ROM_SIZE];

        private readonly void IndexGuard(int lineNumber) {
            if (lineNumber < 0 || lineNumber >= numLines) {
                throw new IndexOutOfRangeException();
            }
        }

        public readonly Line this[int lineNumber]
        {
            get {
                IndexGuard(lineNumber);
                fixed (ulong* dataPtr = &data[lineNumber]) {
                    return *(Line*)dataPtr;
                }
            }
            set {
                IndexGuard(lineNumber);
                fixed (ulong* dataPtr = &data[lineNumber]) {
                    *(Line*)dataPtr = value;
                }
            }
        }
    }
    private PackedROM data = new();
    public Line this[int lineNumber]
    {
        get => data[lineNumber];
        private set => data[lineNumber] = value;
    }
    #endregion

    #region Parse from string
    public static ROM Compile(string code)
    {
        Dictionary<string, ushort> labels = new();
        int offset = 0;
        var lines = code
            .Split('\n')
            .Select(line => line.Split(';')[0].Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line
                .Split(null)
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .Select(token => token.Trim())
            )
            .Where((line, i) => {
                string label = line.First();
                bool isLabel = label.StartsWith('.') && label.EndsWith(':');
                if (isLabel) {
                    labels.Add(label, (ushort)(i - offset++));
                }
                return !isLabel;
            })
            .Select((line, i) => (line, i));

        ROM rom = new();
        foreach (var (line, i) in lines)
        {
            string token0 = line.Take(1).Single();
            var op = (Instruction)Enum.Parse(typeof(Instruction), token0.ToUpper());

            string[] args = line.Take(3).ToArray();
            int numArgs = args.Length;

            string roja = (numArgs > 0) ? args[0] : null;

            string arg1 = (numArgs > 1) ? args[1] : null;
            bool is1Imm = arg1?.StartsWith('#') ?? false;

            string arg2 = (numArgs > 2) ? args[2] : null;
            bool is2Imm = arg2?.StartsWith('#') ?? false;

            rom[i] = new Line(
                new Opcode(op, is1Imm, is2Imm),
                (short)RegisterIndex(roja),
                (short)RegisterIndex(arg1),
                (short)RegisterIndex(arg2)
            );
        }
        return rom;
    }
    #endregion
}

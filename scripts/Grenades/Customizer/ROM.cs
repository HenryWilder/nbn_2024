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
    public ROM(IEnumerable<Line> iter)
    {
        data = new PackedROM(iter);
    }

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

        public PackedROM(IEnumerable<Line> iter) {
            fixed (ulong* dataPtr = data) {
                foreach (var line in iter) {
                    *(Line*)dataPtr[numLines++] = line;
                }
            }
        }
        
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
    private PackedROM data;
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
        return new(code
            .Split('\n')
            .Select(line => line.Split(';')[0].Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line
                .Split(null)
                .Select(token => token.Trim())
                .Where(token => !string.IsNullOrEmpty(token))
            )
            .Where((line, i) => {
                string label = line.First();
                bool isLabel = label.StartsWith('.') && label.EndsWith(':');
                if (isLabel) {
                    labels.Add(label, (ushort)(i - offset++));
                }
                return !isLabel;
            })
            .Select(line => (
                op: (Instruction)Enum.Parse(typeof(Instruction), line.Take(1).Single().ToUpper()),
                args: line.Take(3)
            ))
            .Select(line => (
                line.op,
                roja: line.args.Take(1).SingleOrDefault(),
                arg1: line.args.Take(1).SingleOrDefault(),
                arg2: line.args.Take(1).SingleOrDefault()
            ))
            .Select(line => (
                line.op,
                line.roja,
                line.arg1,
                line.arg2,
                is1Imm: line.arg1 is not null && line.arg1.StartsWith('#'),
                is2Imm: line.arg2 is not null && line.arg2.StartsWith('#')
            ))
            .Select((line, i) => {
                return new Line(
                    new Opcode(line.op, line.is1Imm, line.is2Imm),
                    (short)RegisterIndex(line.roja),
                    (short)RegisterIndex(line.arg1),
                    (short)RegisterIndex(line.arg2)
                );
            })
        );
    }
    #endregion

    #region Example ROM
    public static readonly ROM ExampleROM = Compile(
        "mov r0 300      ;set the timer to repeat 300 times\n" +
        "                ;note that reps are not a measure of time\n" +
        ".timer:         ;define a spot to jump back to later\n" +
        "    nop         ;do nothing this cycle\n" +
        "    sub r0 r0 1 ;decrement the timer\n" +
        "    jnz .timer  ;repeat if the CPU's zero flag is set\n" +
        "                ;(i.e. the most recent operation resulted in 0)\n" +
        "    mov 1 bam   ;explode the grenade\n"
    );
    #endregion
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Godot;
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

        /// <summary> Alias of roja </summary>
        public short Result => roja;

        /// <summary> Alias of roja </summary>
        public short JumpAddress => roja;

        public override string ToString()
        {
            var rojaLbl = IsJump(opcode.Operation) ? "lbl" : "reg";
            var arg1Imm = opcode.IsArg1Immediate ? "lit" : "reg";
            var arg2Imm = opcode.IsArg2Immediate ? "lit" : "reg";
            return $"{opcode.Operation} {rojaLbl}({roja}) {arg1Imm}({arg1}) {arg2Imm}({arg2})";
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
                    *(Line*)&dataPtr[numLines++] = line;
                }
            }
        }

        public readonly int numLines = 0; // May be fewer than the allocated space
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

    public int NumLines => data.numLines;

    #region Parse from string
    public static ROM Compile(string code)
    {
        GD.Print($"Compiling source code:\n```\n{code}\n```");

        Dictionary<string, short> labels = new();

        short LabelLookup(string label) {
            if (labels.TryGetValue(label, out short lineNumber)) {
                return lineNumber;
            } else {
                GD.PrintErr($"label \"{label}\" is not defined");
                return short.MaxValue;
            }
        }

        static bool IsImmediate(string arg) =>
            arg is null || arg[0] == '#';

        short ParseRoJA(string roja) =>
            roja is not null
                ? roja[0] == '.'
                    ? LabelLookup(roja)
                    : Registers.Lookup(roja)
                : (short)0;

        static short ParseArg(bool isImmediate, string arg) =>
            arg is not null
                ? isImmediate
                    ? short.Parse(arg[1..])
                    : Registers.Lookup(arg)
                : (short)0;

        int offset = 0;
        return new(code
            // iterate over each line
            .Split('\n')
            // remove comments
            .Select(line => line.Split(';')[0])
            // skip blank lines without contributing to the line number
            .Where(line => !string.IsNullOrWhiteSpace(line))
            // break line into tokens
            .Select(line => line
                .Split(null)
                .Select(token => token.Trim())
                .Where(token => !string.IsNullOrWhiteSpace(token))
            )
            // map labels and remove them from the source code
            .Where((line, i) => {
                string label = line.First();
                bool isLabel = label.StartsWith('.') && label.EndsWith(':');
                if (isLabel) {
                    short position = (short)(i - offset++);
                    string labelName = label[..(label.Length - 1)];
                    // GD.Print($"mapping label \"{labelName}\" to line {position}");
                    labels.Add(labelName, position);
                }
                return !isLabel;
            })
            .Select((line, i) => {
                var token0 = line.First();
                var op = (Instruction)Enum.Parse(typeof(Instruction), token0.ToUpper());

                var args = line.Skip(1).Take(3);
                // GD.Print($"args: [{string.Join(',', args)}]");
                var roja = args.ElementAtOrDefault(0);
                var arg1 = args.ElementAtOrDefault(1);
                var arg2 = args.ElementAtOrDefault(2);

                bool is1Imm = IsImmediate(arg1);
                bool is2Imm = IsImmediate(arg2);

                var result = new Line(
                    new Opcode(op, is1Imm, is2Imm),
                    ParseRoJA(roja),
                    ParseArg(is1Imm, arg1),
                    ParseArg(is2Imm, arg2)
                );
                // GD.Print($"result: {result}");
                return result;
            })
        );
    }
    #endregion

    public override string ToString()
    {
        return string.Join('\n', Enumerable.Range(0, NumLines).Select(i => $"{i}: {this[i]}"));
    }

    #region Example ROM
    public static readonly ROM ExampleROM = Compile(
        "mov r0 #2        ;set the timer to repeat 2 times\n" +
        "                 ;note that reps are not a measure of time\n" +
        ".timer:          ;define a spot to jump back to later\n" +
        "    nop          ;do nothing this cycle\n" +
        "    sub r0 r0 #1 ;decrement the timer\n" +
        "    jnz .timer   ;repeat if the CPU's zero flag is set\n" +
        "                 ;(i.e. the most recent operation resulted in 0)\n" +
        "    mov bam #1   ;explode the grenade"
    );
    #endregion
}

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
            var op = opcode.Operation;
            int num = op.ExpectedArgs();
            List<string> parts = new(1 + num) { $"{op}" };
            if (num > 0) parts.Add((op.IsJump() ? "lbl" : "reg") + $"({roja})");
            if (num > 1) parts.Add((opcode.IsArg1Immediate ? "lit" : "reg") + $"({arg1})");
            if (num > 2) parts.Add((opcode.IsArg2Immediate ? "lit" : "reg") + $"({arg2})");
            return string.Join(' ', parts);
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
    public static ROM Parse(string code)
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

        var lines = code
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
            .Where((line, i) =>
            {
                string label = line.First();
                bool isLabel = label.StartsWith('.') && label.EndsWith(':');
                if (isLabel)
                {
                    short position = (short)(i - offset++);
                    string labelName = label[..(label.Length - 1)];
                    // GD.Print($"mapping label \"{labelName}\" to line {position}");
                    labels.Add(labelName, position);
                }
                return !isLabel;
            })
            // "collect" the iterator so that every element is evaluated.
            // we need the labels to be mapped before looking them up;
            // and we want to be able to jump forward, not just back.
            .ToArray();

        var result = lines
            .Select((line, i) =>
            {
                var token0 = line.First();
                var op = Enum.Parse<Instruction>(token0.ToUpper());

                var args = line.Skip(1).Take(3);
                GD.Print($"args: [{string.Join(',', args)}]");
                int numExpected = op.ExpectedArgs();
                if (args.Count() != numExpected)
                {
                    GD.PrintErr($"at line {i}: {op} expects {numExpected} arguments but {args.Count()} were provided");
                }
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
            });

        ROM rom = new(result);
        GD.Print($"Generated ROM with {rom.NumLines*sizeof(ulong)}/{PackedROM.ROM_SIZE*sizeof(ulong)} bytes");
        return rom;
    }
    #endregion

    public override string ToString()
    {
        return string.Join('\n', Enumerable.Range(0, NumLines).Select(i => $"{i}: {this[i]}"));
    }

    #region Example ROM
    public static readonly ROM ExampleROM = Parse(@"
mov r0 #2        ;set the timer to repeat 2 times
                 ;note that reps are not a measure of time
.timer:          ;define a spot to jump back to later
    nop          ;do nothing this cycle
    sub r0 r0 #1 ;decrement the timer
    jnz .timer   ;repeat if the CPU's zero flag is set
                 ;(i.e. the most recent operation resulted in 0)
    mov bam #1   ;explode the grenade
");
    #endregion
}

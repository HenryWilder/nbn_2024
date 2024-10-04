using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Godot;
using static Device.CPU;
using static DebugUtility;

/// <summary>
/// Program memory
/// </summary>
public class ROM : IRichDisplay
{
    public ROM(IEnumerable<Line> iter)
    {
        data = new PackedROM(iter);
    }

    #region Line
    [StructLayout(LayoutKind.Sequential, Pack = sizeof(short))]
    public readonly struct Line(Opcode opcode, short roja, short arg1, short arg2) : IRichDisplay
    {
        public readonly Opcode opcode = opcode;
        /// <summary> Result or jump address </summary>
        public readonly short roja = roja;
        public readonly short arg1 = arg1;
        public readonly short arg2 = arg2;

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

        public string ToRich()
        {
            var op = opcode.Operation;
            int num = op.ExpectedArgs();


            List<string> parts = new(1 + num) { op.Highlight(Syntax.Keyword) };
            if (num > 0) parts.Add(RichUnion(op.IsJump() ? ("lbl", Syntax.Function) : ("reg", Syntax.Variable), roja));
            if (num > 1) parts.Add(RichUnion(opcode.IsArg1Immediate ? ("lit", Syntax.NumLiteral) : ("reg", Syntax.Variable), arg1));
            if (num > 2) parts.Add(RichUnion(opcode.IsArg2Immediate ? ("lit", Syntax.NumLiteral) : ("reg", Syntax.Variable), arg2));
            return string.Join(' ', parts);
        }

        public string Descriptive()
        {
            var op = opcode.Operation;

            var rojaStr = op.IsJump()
                ? $"line {roja}".Highlight(Syntax.Function)
                : $"register {roja}".Highlight(Syntax.Variable);

            var arg1Str = opcode.IsArg1Immediate
                ? $"{arg1}".Highlight(Syntax.NumLiteral)
                : $"the value in register {arg1}".Highlight(Syntax.Variable);

            var arg2Str = opcode.IsArg2Immediate
                ? $"{arg2}".Highlight(Syntax.NumLiteral)
                : $"the value in register {arg2}".Highlight(Syntax.Variable);

            var (syntax, text) = op switch
            {
                Instruction.NOP => (Syntax.Control, $"do nothing"),
                Instruction.MOV => (Syntax.Keyword, $"set {rojaStr} equal to {arg1Str}"),
                Instruction.LDR => (Syntax.Keyword, $"load RAM at index {arg1Str} into {rojaStr}"),
                Instruction.SDR => (Syntax.Keyword, $"save {rojaStr} to RAM at index {arg1Str}"),
                Instruction.ADD => (Syntax.Keyword, $"calculate {arg1Str} + {arg2Str} and store the result in {rojaStr}"),
                Instruction.SUB => (Syntax.Keyword, $"calculate {arg1Str} - {arg2Str} and store the result in {rojaStr}"),
                Instruction.MUL => (Syntax.Keyword, $"calculate {arg1Str} * {arg2Str} and store the result in {rojaStr}"),
                Instruction.DIV => (Syntax.Keyword, $"calculate {arg1Str} / {arg2Str} and store the result in {rojaStr}"),
                Instruction.AND => (Syntax.Keyword, $"calculate bitwise {arg1Str} AND {arg2Str} and store the result in {rojaStr}"),
                Instruction.ORR => (Syntax.Keyword, $"calculate bitwise {arg1Str} OR {arg2Str} and store the result in {rojaStr}"),
                Instruction.NOT => (Syntax.Keyword, $"calculate bitwise NOT {arg1Str} and store the result in {rojaStr}"),
                Instruction.XOR => (Syntax.Keyword, $"calculate bitwise {arg1Str} XOR {arg2Str} and store the result in {rojaStr}"),
                Instruction.JMP => (Syntax.Control, $"jump to {rojaStr}"),
                Instruction.JE  => (Syntax.Control, $"jump to {rojaStr} if {arg1Str} equals {arg1Str}"),
                Instruction.JNE => (Syntax.Control, $"jump to {rojaStr} if {arg1Str} does not equal {arg1Str}"),
                Instruction.JZ  => (Syntax.Control, $"jump to {rojaStr} if the zero bit is set"),
                Instruction.JNZ => (Syntax.Control, $"jump to {rojaStr} if the zero bit is not set"),
                Instruction.JG  => (Syntax.Control, $"jump to {rojaStr} if {arg1Str} is greater than {arg2Str}"),
                Instruction.JL  => (Syntax.Control, $"jump to {rojaStr} if {arg1Str} is less than {arg2Str}"),
                Instruction.JGE => (Syntax.Control, $"jump to {rojaStr} if {arg1Str} is greater than or equal to {arg2Str}"),
                Instruction.JLE => (Syntax.Control, $"jump to {rojaStr} if {arg1Str} is less than or equal to {arg2Str}"),
                Instruction.JS  => (Syntax.Control, $"jump to {rojaStr} if the sign bit is set"),
                _ => throw new NotImplementedException(),
            };
            return text.Highlight(syntax);
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
        GD.Print($"Parsing assembly:\n```\n{code}\n```");

        Dictionary<string, short> labels = [];

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
                var args = line.Skip(1).Take(3);
                GD.PrintRich(
                    $"line {i}:".Highlight(Syntax.Function) + '\n' +
                    $"  op: {$"\"{token0}\"".Highlight(Syntax.StrLiteral)}\n" +
                    $"  args: [{string.Join(',', args.Select(x => $"\"{x}\"".Highlight(Syntax.StrLiteral)))}]");

                var op = Enum.Parse<Instruction>(token0.ToUpper());
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
                GD.PrintRich("  result: " + result.ToRich());
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

    public string ToRich()
    {
        return string.Join('\n', Enumerable.Range(0, NumLines).Select(i => $"{i}: ".Highlight(Syntax.Function) + this[i].ToRich()));
    }

//     #region Example ROM
//     public static readonly ROM ExampleROM = Parse(@"
// mov r0 #2        ;set the timer to repeat 2 times
//                  ;note that reps are not a measure of time
// .timer:          ;define a spot to jump back to later
//     nop          ;do nothing this cycle
//     sub r0 r0 #1 ;decrement the timer
//     jnz .timer   ;repeat if the CPU's zero flag is set
//                  ;(i.e. the most recent operation resulted in 0)
//     mov bam #1   ;explode the grenade
// ");
//     #endregion
}

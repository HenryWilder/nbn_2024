using System;
using System.Collections.Generic;
using System.Linq;
using static InstructionExtension;

/// <summary>
/// Program memory
/// </summary>
public class ROM
{
    private int numLines = 0;
    private readonly Line[] rom = new Line[512];

    public ROM(string code)
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

        foreach (var (line, i) in lines)
        {
            string token0 = line.Take(1).Single();
            if (!token0.TryIntoInstruction(out var op))
            {
                throw new Exception($"{token0} is not a valid instruction");
            }

            string[] args = line.Take(3).ToArray();
            int numArgs = args.Length;

            string roja = (numArgs > 0) ? args[0] : null;
            bool isJImm = roja?.StartsWith('.') ?? false;

            string arg1 = (numArgs > 1) ? args[1] : null;
            bool is1Imm = arg1?.StartsWith('#') ?? false;

            string arg2 = (numArgs > 2) ? args[2] : null;
            bool is2Imm = arg2?.StartsWith('#') ?? false;

            rom[i] = new Line(
                new Opcode(op, isJImm, is1Imm, is2Imm),
                (short)CPU.RegisterIndex(roja),
                (short)CPU.RegisterIndex(arg1),
                (short)CPU.RegisterIndex(arg2)
            );
        }
    }

    public void AddLine(Line line)
    {
        if (numLines == rom.Length) {
            throw new OutOfMemoryException("ROM cannot store any more lines");
        }
        rom[numLines++] = line;
    }

    public Line GetLine(int n)
    {
        return rom[n];
    }
}

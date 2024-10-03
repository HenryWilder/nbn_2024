using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;

/// <summary>
/// Interpreted program
/// </summary>
public class NadeBasic
{
    NadeBasic(IEnumerable<Token> tokens)
    {
        this.tokens = tokens.ToArray();
    }

    public enum Cmd : short
    {
        End, // ends a region like "for" or "if", not the entire program
        For,
        Explode,
    }

    public enum Punc : short
    {
        LineBreak = (short)':',
        Comma = (short)',',
        LParen = (short)'(', // distinct from the lparen baked into parameterized commands
        RParen = (short)')',
    }

    public unsafe readonly struct Token
    {
        private Token(Tag tag, short data)
        {
            this.tag = tag;
            this.data = data;
        }

        public enum Tag : short
        {
            Command,
            Literal, // short.MinValue-short.MaxValue
            Variable, // A-Z
            Punctuation,
        }

        private readonly Tag tag;
        private readonly short data;

        public static Token Command(Cmd cmd)
            => new(Tag.Command, (short)cmd);

        public static Token Literal(short value)
            => new(Tag.Literal, value);

        public static Token Variable(char name)
            => new(Tag.Variable, (short)name);

        public static Token Punctuation(Punc punc)
            => new(Tag.Punctuation, (short)punc);

        public static Token LineBreak
            => Punctuation(Punc.LineBreak);

        public override string ToString()
        {
            return tag switch {
                Tag.Command => $"cmd({(Cmd)data})",
                Tag.Literal => $"lit({data})",
                Tag.Variable => $"var({(char)data})",
                Tag.Punctuation => $"punc({(Punc)data})",
                _ => throw new NotImplementedException(),
            };
        }
    }
    public readonly Token[] tokens;

    private static readonly Regex rxTokenize = new(
        @"(:|For \(|End|Explode|[A-Z]|-?[0-9]+|,)",
        RegexOptions.Compiled);

    public static NadeBasic Parse(string code)
    {
        GD.Print($"Parsing NadeBasic code:\n```\n{code}\n```");
        var tokens = rxTokenize
            .Matches(code.Replace("\n", ""))
            .Select((Match match) =>
            {
                string word = match.Value;
                switch (word)
                {
                    case ":":
                        return Token.LineBreak;
                    case "For (":
                        return Token.Command(Cmd.For);
                    case "End":
                        return Token.Command(Cmd.End);
                    case "Explode":
                        return Token.Command(Cmd.Explode);
                    case ",":
                        return Token.Punctuation(Punc.Comma);
                    case "(":
                        return Token.Punctuation(Punc.LParen);
                    case ")":
                        return Token.Punctuation(Punc.RParen);
                    default:
                        if (word[0] is char name && char.IsLetter(name) && word.Length == 1)
                        {
                            return Token.Variable(name);
                        }
                        else if ((word[0] == '-' ? word[1] : word[0]) is char d0 && char.IsNumber(d0))
                        {
                            return Token.Literal(short.Parse(word));
                        }
                        else
                        {
                            GD.PrintErr($"Unknown token: \"{word}\"");
                            throw new Exception();
                        }
                }
            });

        NadeBasic result = new(tokens);
        GD.Print($"Generated {result.tokens.Length} tokens:\n```\n{result}\n```");
        return result;
    }

    public override string ToString()
    {
        return string.Join('\n', tokens.Select(token => token.ToString()));
    }

    #region NadeBasic Interpreter ROM
    public static readonly ROM InterpreterROM = ROM.Compile(@"
mov r1 #2 ;token counter
          ;offset added to account for NadeBasic 'header'
ldr r2 #0 ;total number of tokens

.readTokens:
    mul r3 r1 #4          ;calculate byte offset for tag
    ldr r0 r1             ;first token
    add r3 r3 #1          ;calculate byte offset for tag
    add r1 r1 #1          ;increment counter
    jle .readTokens r1 r2 ;while counter <= total

mov bam #1
");
    #endregion

    #region Example Program
    public static readonly NadeBasic ExampleProgram = Parse(@"
:For (I,0,3
:End
:Explode
");
    #endregion
}

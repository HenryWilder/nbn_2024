using System;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;

public static class NadeSy
{
    interface IToken {}

    class Keyword : IToken {
        public enum Kw {
            If,
            Else,
            For,
            In,
            While,
            Let,
            Const,
            Range, // ..
        }
        public Kw value;
    }

    class Variable : IToken {
        public string name;
    }

    class Literal : IToken {
        public short value;
    }

    class Operator : IToken {
        public enum Op {
            Add,
            Sub,
            Mul,
            Div,
            Rem,
            BitAnd,
            BitOr,
            BitXor,
            BitNot,
            AddAssign,
            SubAssign,
            MulAssign,
            DivAssign,
            RemAssign,
            BitAndAssign,
            BitOrAssign,
            BitXorAssign,
            BitNotAssign,
            LogicAnd,
            LogicOr,
            LogicXor,
            LogicNot,
            Assign,
            Eq,
            Ne,
            Gt,
            Ge,
            Lt,
            Le,
            Implies,
        }
        public Op op;
    }

    private static readonly Regex rxComments = new(
        @"//.*?\n|/\*.*?\*/",
        RegexOptions.Compiled);
    private static readonly Regex rxExcessSpaces = new(
        @"\s{2,}",
        RegexOptions.Compiled);

    private static readonly Regex rxTokenize = new(
        @"\b(?:if|else|for|while|let|const|in|[a-zA-Z_][a-zA-Z_0-9]*|[0-9]+)\b|\.\.|[(){}[\]]|<<|>>|[-+*/%!<=>^&|~]=|=>|&&|\|\||[-+*/%!<=>^&|~;]",
        RegexOptions.Compiled);

    public static ROM Compile(string code)
    {
        GD.Print($"Compiling source code:\n```\n{code}\n```");
        code = rxComments.Replace(code, "");
        code = code.Replace('\n', ' ');
        code = rxExcessSpaces.Replace(code, " ");
        var tokens = rxTokenize.Matches(code).Select(match => match.Value);
        GD.Print($"Tokens: [\n{string.Join('\n', tokens.Select(token => "  " + token))}\n]");

        return ROM.Parse(";todo");
    }

    public static readonly ROM ExampleSy = Compile(@"
const N = 4;
for i in 0..N {
    // do nothing
}
explode();
");
}

using System;
using System.Linq;

public static class DebugUtility
{
    public enum Syntax
    {
        Typename,
        Function,
        Variable,
        Constant,
        BracketGold,
        BracketPink,
        BracketBlue,
        Keyword,
        Control,
        NumLiteral,
        StrLiteral,
        StrEscape,
        Comment,
        RegexRed,
        RegexPaleYellow,
        RegexGold,
        Error,
        Warning,
    }

    public static string Highlight(this object text, Syntax syntax)
    {
        string colorCode = syntax switch {
            Syntax.Typename        => "#4ec9b0",
            Syntax.Function        => "#dcdcaa",
            Syntax.Variable        => "#9cdcfe",
            Syntax.Constant        => "#4fc1ff",
            Syntax.BracketGold     => "#ffd700",
            Syntax.BracketPink     => "#da70d6",
            Syntax.BracketBlue     => "#179fff",
            Syntax.Keyword         => "#569cd6",
            Syntax.Control         => "#c586c0",
            Syntax.NumLiteral      => "#b5cea8",
            Syntax.StrLiteral      => "#ce9178",
            Syntax.StrEscape       => "#d7ba7d",
            Syntax.Comment         => "#6a9955",
            Syntax.RegexRed        => "#d16969",
            Syntax.RegexPaleYellow => "#dcdcaa",
            Syntax.RegexGold       => "#d7ba7d",
            Syntax.Error           => "#f85149",
            Syntax.Warning         => "#cca700",
            _ => throw new NotImplementedException(),
        };
        return string.Join('\n', $"{text}".Split('\n').Select(line => $"[color={colorCode}]{line}[/color]"));
    }

    public interface IRichDisplay
    {
        public string ToRich();
    }

    public static string RichUnion(string variant, object value)
    {
        return variant.Highlight(Syntax.Typename) + $"({value})";
    }

    public static string RichUnion((string name, Syntax syntax) variant, object value)
    {
        return RichUnion(variant.name, value.Highlight(variant.syntax));
    }
}

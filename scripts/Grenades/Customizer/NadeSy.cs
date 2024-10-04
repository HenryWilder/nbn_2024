using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;

public static class NadeSy
{
    #region Token
    interface IToken
    {
        public static IToken ParseWord(string word) => word switch
        {
            _ when char.IsDigit(word[0])
                => new NumLiteral(short.Parse(word)),
            "else if"
                => new Keyword(Kw.ElseIf),
            "if"
                => new Keyword(Kw.If),
            "else"
                => new Keyword(Kw.Else),
            "for"
                => new Keyword(Kw.For),
            "in"
                => new Keyword(Kw.In),
            "while"
                => new Keyword(Kw.While),
            "let"
                => new Keyword(Kw.Let),
            "const"
                => new Keyword(Kw.Const),
            _ when char.IsLetter(word[0]) || word[0] == '_'
                => new Variable(word),
            "+"
                => new Operator(Op.Add),
            "-"
                => new Operator(Op.Sub),
            "*"
                => new Operator(Op.Mul),
            "/"
                => new Operator(Op.Div),
            "%"
                => new Operator(Op.Rem),
            "&"
                => new Operator(Op.BitAnd),
            "|"
                => new Operator(Op.BitOr),
            "^"
                => new Operator(Op.BitXor),
            "~"
                => new Operator(Op.BitNot),
            "+="
                => new Operator(Op.AddAssign),
            "-="
                => new Operator(Op.SubAssign),
            "*="
                => new Operator(Op.MulAssign),
            "/="
                => new Operator(Op.DivAssign),
            "%="
                => new Operator(Op.RemAssign),
            "&="
                => new Operator(Op.BitAndAssign),
            "|="
                => new Operator(Op.BitOrAssign),
            "^="
                => new Operator(Op.BitXorAssign),
            "&&"
                => new Operator(Op.LogicAnd),
            "||"
                => new Operator(Op.LogicOr),
            "!"
                => new Operator(Op.LogicNot),
            "="
                => new Operator(Op.Assign),
            "=="
                => new Operator(Op.Eq),
            "!="
                => new Operator(Op.Ne),
            ">"
                => new Operator(Op.Gt),
            ">="
                => new Operator(Op.Ge),
            "<"
                => new Operator(Op.Lt),
            "<="
                => new Operator(Op.Le),
            "=>"
                => new Operator(Op.Implies),
            ".."
                => new Operator(Op.Range),
            ","
                => new ScopeControl(ScopeType.Inline, ScopeDirection.Pop),
            ";"
                => new ScopeControl(ScopeType.Statement, ScopeDirection.Pop),
            "("
                => new ScopeControl(ScopeType.Expression, ScopeDirection.Push),
            ")"
                => new ScopeControl(ScopeType.Expression, ScopeDirection.Pop),
            "{"
                => new ScopeControl(ScopeType.Scope, ScopeDirection.Push),
            "}"
                => new ScopeControl(ScopeType.Scope, ScopeDirection.Pop),
            "["
                => new ScopeControl(ScopeType.Subscript, ScopeDirection.Push),
            "]"
                => new ScopeControl(ScopeType.Subscript, ScopeDirection.Pop),
            _
                => throw new NotImplementedException($"Unknown token: \"{word}\""),
        };
    }

    #region Keyword
    public enum Kw
    {
        If,
        Else,
        ElseIf,
        For,
        In,
        While,
        Let,
        Const,
    }
    class Keyword(Kw kw) : IToken
    {
        public Kw kw = kw;
        public override string ToString() => $"[color=#4ec9b0]kw[/color]([color=#c586c0]{kw}[/color])";
    }
    #endregion

    #region Variable
    class Variable(string name) : IToken
    {
        public string name = name;
        public override string ToString() => $"[color=#4ec9b0]var[/color]([color=#9cdcfe]{name}[/color])";
    }

    #endregion

    #region Numeric Literal

    class NumLiteral(short value) : IToken
    {
        public short value = value;
        public override string ToString() => $"[color=#4ec9b0]num[/color]([color=#b5cea8]{value}[/color])";
    }
    #endregion

    #region Operator
    public enum Op
    {
        /// <summary> <c>+</c> </summary>
        Add,
        /// <summary> <c>-</c> </summary>
        Sub,
        /// <summary> <c>*</c> </summary>
        Mul,
        /// <summary> <c>/</c> </summary>
        Div,
        /// <summary> <c>%</c> </summary>
        Rem,
        /// <summary> <c>&</c> </summary>
        BitAnd,
        /// <summary> <c>|</c> </summary>
        BitOr,
        /// <summary> <c>^</c> </summary>
        BitXor,
        /// <summary> <c>~</c> </summary>
        BitNot,
        /// <summary> <c>+=</c> </summary>
        AddAssign,
        /// <summary> <c>-=</c> </summary>
        SubAssign,
        /// <summary> <c>*=</c> </summary>
        MulAssign,
        /// <summary> <c>/=</c> </summary>
        DivAssign,
        /// <summary> <c>%=</c> </summary>
        RemAssign,
        /// <summary> <c>&=</c> </summary>
        BitAndAssign,
        /// <summary> <c>|=</c> </summary>
        BitOrAssign,
        /// <summary> <c>^=</c> </summary>
        BitXorAssign,
        /// <summary> <c>&&</c> </summary>
        LogicAnd,
        /// <summary> <c>||</c> </summary>
        LogicOr,
        /// <summary> <c>!</c> </summary>
        LogicNot,
        /// <summary> <c>=</c> </summary>
        Assign,
        /// <summary> <c>==</c> </summary>
        Eq,
        /// <summary> <c>!=</c> </summary>
        Ne,
        /// <summary> <c>></c> </summary>
        Gt,
        /// <summary> <c>>=</c> </summary>
        Ge,
        /// <summary> <c><</c> </summary>
        Lt,
        /// <summary> <c><=</c> </summary>
        Le,
        /// <summary> <c>=></c> </summary>
        Implies,
        /// <summary> <c>..</c> </summary>
        Range,
    }
    class Operator(Op op) : IToken
    {
        public Op op = op;
        public override string ToString() => $"[color=#4ec9b0]op[/color]([color=#dcdcaa]{op}[/color])";
    }
    #endregion

    #region Scope
    enum ScopeType
    {
        /// <summary>
        /// <c>(...)</c>
        /// Part of a statement, whose contents get higher precedence than if they weren't grouped.
        /// </summary>
        Expression,

        /// <summary>
        /// <c>[...]</c>
        /// </summary>
        Subscript,

        /// <summary>
        /// An expression without parentheses---a list item.
        /// Implied by there being an <c>Expression</c> or <c>Indexer</c>.<br/>
        /// Separated by <c>,</c> and popped automatically when popping out of a containing scope.
        /// </summary>
        Inline,

        /// <summary>
        /// <c>{...}</c>
        /// A collection of statements.
        /// </summary>
        Scope,

        /// <summary>
        /// Basically a "line".
        /// Implied by there being a <c>Scope</c>.<br/>
        /// Separated by <c>;</c> and popped automatically when popping out of a containing scope.
        /// </summary>
        Statement,
    }
    enum ScopeDirection
    {
        Push,
        Pop,
    }
    class ScopeControl(ScopeType tag, ScopeDirection dir) : IToken
    {
        public ScopeType tag = tag;
        public ScopeDirection dir = dir;
        public override string ToString() => $"[color=#4ec9b0]scope[/color]([color=#4fc1ff]{tag}.{dir}[/color])";
    }
    #endregion

    #endregion

    #region Tokenizer
    private static readonly Regex rxComments = new(
        @"//.*?\n|/\*.*?\*/",
        RegexOptions.Compiled);
    private static readonly Regex rxExcessSpaces = new(
        @"\s{2,}",
        RegexOptions.Compiled);

    // language=regex
    private static readonly string RX_KEYWORD_STR = string.Join('|', [
        @"else if", // must come before `if` and `else`
        @"if",
        @"let",
        @"const",
        @"else",
        @"for",
        @"while",
        @"in",
    ]);

    // language=regex
    private const string RX_VARNAME_STR = @"[a-zA-Z_][a-zA-Z_0-9]*";

    // language=regex
    private const string RX_NUM_LITERAL_STR = @"[0-9]+";

    // language=regex
    private const string RX_SCOPING_STR = @"[(){}[\]]";

    // language=regex
    private static readonly string RX_OPERATOR_STR = string.Join('|', [
        @"\.\.",
        @"<<", @">>", @"&&", @"\|\|", @"=>",
        @"[-+*/%=!><^|&]=",
        @"[-+*/%=!><^|&~;]"
    ]);

    private static readonly Regex rxTokenize = new(
        @$"\b(?:{RX_KEYWORD_STR}|{RX_VARNAME_STR}|{RX_NUM_LITERAL_STR})\b|{RX_SCOPING_STR}|{RX_OPERATOR_STR}|\S",
        RegexOptions.Compiled);

    private static IToken[] Tokenize(string code)
    {
        code = rxComments.Replace(code, "");
        code = code.Replace('\n', ' ');
        code = rxExcessSpaces.Replace(code, " ");
        return rxTokenize
            .Matches(code)
            .Select(IToken (match, i) =>
            {
                string word = match.Value;
                try
                {
                    var token = IToken.ParseWord(word);
                    GD.PrintRich($"[color=#ce9178]\"{word}\"[/color] => {token}");
                    return token;
                }
                catch
                {
                    GD.PrintRich($"[color=#ce9178]\"{word}\"[/color] => [color=#d16969][err][/color]");
                    throw;
                }
            })
            .ToArray();
    }
    #endregion

    #region Parser

    private static TokenTree Parse(IEnumerable<IToken> tokens)
    {
        TokenTree.Builder builder = new();
        foreach (var token in tokens)
        {
            builder.PushToken(token);
        }
        return builder.Build();
    }

    #region Token Tree
    class TokenTree
    {
        public interface INode { }

        public interface IExprItem : INode { }

        public class Atom(IToken token) : INode, IExprItem
        {
            public readonly IToken token = token;
            public override string ToString() => $"{token}";
        }

        public abstract class Layer : INode
        {
            public static Layer MakeFrom(ScopeType tag)
                => tag switch
                {
                    ScopeType.Expression or ScopeType.Inline => new SubExpr(tag),
                    ScopeType.Scope => new Scope(),
                    ScopeType.Statement => new Statement(),
                    _ => throw new NotImplementedException($"todo: {tag}"),
                };

            public abstract ScopeType Tag { get; }
            public abstract IEnumerable<INode> Items { get; }

            protected T TryCast<T>(INode what) where T : INode
            {
                if (what is T item) return item;
                else throw new SyntaxErrorException($"{what} is not valid in {Tag}");
            }
            public abstract void Push(INode what);

            protected string InnerString()
            {
                string inner = string.Join("", Items.Select(x => $"\n{x},")).Replace("\n", "\n  ");
                return !string.IsNullOrEmpty(inner) ? inner + '\n' : string.Empty;
            }
            public abstract string Identifier(string content);
            public override string ToString()
                => Identifier(InnerString());
        }

        public class SubExpr(ScopeType tag) : Layer, IExprItem
        {
            public override ScopeType Tag => tag;
            public override IEnumerable<INode> Items => items;
            public override void Push(INode what) => items.Add(TryCast<IExprItem>(what));
            public readonly List<IExprItem> items = [];
            public override string Identifier(string content)
                => $"[color=#ffd700]([/color]{content}[color=#ffd700])[/color]";
        }

        // A statement can contain anything
        public class Statement : Layer
        {
            public override ScopeType Tag => ScopeType.Statement;
            public override IEnumerable<INode> Items => items;
            public override void Push(INode what) => items.Add(what);
            public readonly List<INode> items = [];

            public override string Identifier(string content)
                => $"[color=#179fff]:[[/color]{content}[color=#179fff]];[/color]";
        }

        public class Scope : Layer
        {
            public override ScopeType Tag => ScopeType.Scope;
            public override IEnumerable<INode> Items => items;
            public override void Push(INode what) => items.Add(TryCast<Statement>(what));
            public readonly List<Statement> items = [];

            public override string Identifier(string content)
                => $"[color=#da70d6]{{[/color]{content}[color=#da70d6]}}[/color]";
        }

        public readonly Scope globalScope = new();

        public override string ToString() => globalScope.ToString();

        #region Builder
        public class Builder
        {
            public Builder()
            {
                tree = new();
                scopeStack = new();
                scopeStack.Push((tree.globalScope, 0));
            }

            readonly TokenTree tree;
            readonly Stack<(Layer, int)> scopeStack;

            public Layer CurrentScope => scopeStack.First().Item1;

            private static string PathFormat<T, U>(IEnumerable<T> path, Func<T, (Layer, U)> splitter) =>
                string.Join("->", path.Select((x) => {
                    var (layer, inner) = splitter(x);
                    return layer.Identifier(inner.ToString());
                }));

            private static string PathFormat<T>(IEnumerable<T> path, Func<T, Layer> splitter) =>
                PathFormat(path, x => (splitter(x), string.Empty));

            private string StackPath =>
                PathFormat(scopeStack.AsEnumerable().Reverse(), (layer) => (layer.Item1, layer.Item2));

            private static ScopeType[] ImpliedPath(ScopeType tag)
                => tag switch
                {
                    ScopeType.Scope
                        => [tag, ScopeType.Statement],

                    ScopeType.Expression or ScopeType.Subscript
                        => [tag, ScopeType.Inline],

                    _ => [tag],
                };

            public void PushAtom(IToken token)
            {
                GD.PrintRich($"{StackPath} += {token}");
                Atom newAtom = new(token);
                CurrentScope.Push(newAtom);
            }

            private void PushScopes(ScopeType[] tags)
            {
                foreach (var tag in tags)
                {
                    var newScope = Layer.MakeFrom(tag);
                    int i = CurrentScope.Items.Count(x => x is Layer);
                    CurrentScope.Push(newScope);
                    scopeStack.Push((newScope, i));
                }
            }

            private void PopScopes(ScopeType[] tags)
            {
                foreach (var tag in tags.Reverse())
                {
                    var currentScope = CurrentScope;
                    if (scopeStack.Count == 1)
                    {
                        throw new SyntaxErrorException($"Cannot pop global scope");
                    }
                    else if (currentScope.Tag != tag)
                    {
                        throw new SyntaxErrorException($"Cannot pop {tag} scope when current scope is {currentScope.Tag}");
                    }
                    scopeStack.Pop();
                }
                // closing a statement implicitly ends the soft statement
                if (tags.First() is ScopeType.Scope && CurrentScope.Tag == ScopeType.Statement)
                {
                    PopScope(ScopeType.Statement);
                }
            }

            public void PushScope(ScopeType tag)
            {
                var path = ImpliedPath(tag);
                GD.PrintRich($"{StackPath} += {PathFormat(path, Layer.MakeFrom)}");
                PushScopes(path);
            }

            public void PopScope(ScopeType tag)
            {
                var path = ImpliedPath(tag);
                GD.PrintRich($"{StackPath} -= {PathFormat(path, Layer.MakeFrom)}");
                PopScopes(path);
            }

            public void PushToken(IToken token)
            {
                var missing = ImpliedPath(CurrentScope.Tag).Skip(1).ToArray();
                if (missing.Length > 0) PushScopes(missing);
                if (token is ScopeControl { tag: var tag, dir: var dir })
                {
                    switch (dir)
                    {
                        case ScopeDirection.Push: PushScope(tag); break;
                        case ScopeDirection.Pop: PopScope(tag); break;
                    }
                }
                else
                {
                    PushAtom(token);
                }
            }

            public TokenTree Build() => tree;
        }
        #endregion
    }
    #endregion

    #endregion

    #region Concoctor
    interface IConcoction {}

    /// <summary>
    /// kw(If), ($cond1), {$then1},
    /// kw(ElseIf), ($cond2), {$then2},
    /// ...
    /// kw(Else), {$ifNone}
    /// </summary>
    class Conditional : IConcoction
    {
        readonly public List<(object cond, TokenTree.Scope then)> condThen = [];
        public TokenTree.Scope ifNone = null;

        public string ToAsm(string uniqueIdentifier)
        {
            var parts = condThen.SelectMany(IEnumerable<string> (item, n) => [
                $"{item.cond}",
                $"jz .else_{uniqueIdentifier}_{n}",
                $"{item.then}",
                $"jmp .finally_{uniqueIdentifier}",
                $".else_{uniqueIdentifier}_{n}:",
            ]);
            if (ifNone is not null) {
                parts = parts.Concat([
                    $"{ifNone}",
                ]);
            }
            parts = parts.Concat([
                $".finally_{uniqueIdentifier}:",
            ]);
            return string.Join('\n', parts);
        }
    }

    private static IEnumerable<IConcoction> Concoct(TokenTree tree)
    {
        List<IConcoction> items = [];
        foreach (var statement in tree.globalScope.items)
        {
            GD.PrintRich(statement);
            IEnumerable<TokenTree.INode> iter = statement.items;
            switch (iter.FirstOrDefault())
            {
                case TokenTree.Atom { token: Keyword { kw: var kw } }:
                    static IEnumerable<TokenTree.INode> RipConditionOutOf(ref IEnumerable<TokenTree.INode> iter)
                    {
                        var cond = iter.TakeWhile(x => x is not TokenTree.Scope);
                        int num = cond.Count();
                        if (num == 0)
                            throw new SyntaxErrorException("If statement must ask something");
                        iter = iter.Skip(num);
                        return cond;
                    }
                    static TokenTree.Scope RipScopeOutOf(ref IEnumerable<TokenTree.INode> iter)
                    {
                        if (iter.FirstOrDefault() is not TokenTree.Scope then)
                            throw new SyntaxErrorException("If statement must do something");
                        iter = iter.Skip(1);
                        return then;
                    }
                    switch (kw) {
                        case Kw.If:
                            {
                                GD.PrintRich("If statement");
                                var cond = RipConditionOutOf(ref iter);
                                GD.PrintRich($"{cond.Count()} token condition: {string.Join(',',cond)}");
                                var then = RipScopeOutOf(ref iter);
                                GD.PrintRich($"if true: {then}");
                                items.Add(new Conditional() {
                                    condThen = { (cond, then) }
                                });
                            }
                            break;

                        case Kw.ElseIf:
                            break;

                        case Kw.Else:
                            break;

                        default:
                            GD.PrintErr($"todo: {kw}");
                            break;
                    }
                    break;

                default:
                    GD.PrintErr("I don't know what this is...");
                    break;
            }
        }
        return items;
    }
    #endregion

    #region Compiler
    public static ROM Compile(string code)
    {
        GD.PrintRich($"Compiling source code:\n```\n{code}\n```");
        try
        {
            GD.PrintRich("Tokenizing...");
            var tokens = Tokenize(code);

            GD.PrintRich("Building token tree...");
            var tree = Parse(tokens);
            GD.PrintRich($"Generated token tree:\n{tree}\n");

            GD.PrintRich("Concocting...");
            var concoction = Concoct(tree);
            GD.PrintRich($"Generated concoction: {concoction}");

            throw new NotImplementedException("todo: convert concoction into assembly");
            // return ROM.Parse(";todo");
        }
        catch (Exception e)
        {
            GD.PrintErr($"Emulator compile error: {e.Message}\nNo ROM was generated");
            return null;
        }
    }
    #endregion

    #region Example ROM
    public static readonly ROM ExampleSy = Compile(@"
let a = 5;
if a == 3 {
    // :3
} else if a == 6 {
    // :3
} else {
    // :3
}
const NUM_LOOPS = 4;
for (i in 0..NUM_LOOPS /* loop from 0 to NUM_LOOPS (i.e. 4) */) {
    // do nothing
}
explode();
");
    #endregion
}

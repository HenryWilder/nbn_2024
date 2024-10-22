using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Godot;
using static DebugUtility;

public static class NadeSy
{
    #region Token
    interface IToken : IRichDisplay {}

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
        public string ToRich()
            => RichUnion(("kw", Syntax.Control), kw);
    }
    #endregion

    #region Variable
    class Variable(string name) : IToken
    {
        public string name = name;
        public string ToRich()
            => RichUnion(("var", Syntax.Variable), name);
    }

    #endregion

    #region Numeric Literal

    class NumLiteral(short value) : IToken
    {
        public short value = value;
        public string ToRich()
            => RichUnion(("num", Syntax.NumLiteral), value);
    }
    #endregion

    #region Operator
    public enum Op
    {
        /// <summary> <c>+</c> </summary>
        Add,
        /// <summary> <c>-</c> </summary>
        Subtract,
        /// <summary> <c>*</c> </summary>
        Multiply,
        /// <summary> <c>/</c> </summary>
        Divide,
        /// <summary> <c>%</c> </summary>
        Remainder,
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
        SubtractAssign,
        /// <summary> <c>*=</c> </summary>
        MultiplyAssign,
        /// <summary> <c>/=</c> </summary>
        DivideAssign,
        /// <summary> <c>%=</c> </summary>
        RemainderAssign,
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
        Equal,
        /// <summary> <c>!=</c> </summary>
        NotEqual,
        /// <summary> <c>></c> </summary>
        Greater,
        /// <summary> <c>>=</c> </summary>
        GreaterOrEqual,
        /// <summary> <c><</c> </summary>
        LessThan,
        /// <summary> <c><=</c> </summary>
        LessOrEqual,
        /// <summary> <c>=></c> </summary>
        FatArrow,
        /// <summary> <c>..</c> </summary>
        Range,
    }
    class Operator(Op op) : IToken
    {
        public Op op = op;
        public string ToRich()
            => RichUnion(("op", Syntax.Function), op);
    }
    #endregion

    #region Tokenizer
    private static readonly Tokenizer<IToken> tokenizer = new(
        reserved: [
            ("else if", new Keyword(Kw.ElseIf)),
            ("if",      new Keyword(Kw.If)),
            ("else",    new Keyword(Kw.Else)),
            ("for",     new Keyword(Kw.For)),
            ("in",      new Keyword(Kw.In)),
            ("while",   new Keyword(Kw.While)),
            ("let",     new Keyword(Kw.Let)),
            ("const",   new Keyword(Kw.Const)),
            ("+",       new Operator(Op.Add)),
            ("-",       new Operator(Op.Subtract)),
            ("*",       new Operator(Op.Multiply)),
            ("/",       new Operator(Op.Divide)),
            ("%",       new Operator(Op.Remainder)),
            ("&",       new Operator(Op.BitAnd)),
            ("|",       new Operator(Op.BitOr)),
            ("^",       new Operator(Op.BitXor)),
            ("~",       new Operator(Op.BitNot)),
            ("+=",      new Operator(Op.AddAssign)),
            ("-=",      new Operator(Op.SubtractAssign)),
            ("*=",      new Operator(Op.MultiplyAssign)),
            ("/=",      new Operator(Op.DivideAssign)),
            ("%=",      new Operator(Op.RemainderAssign)),
            ("&=",      new Operator(Op.BitAndAssign)),
            ("|=",      new Operator(Op.BitOrAssign)),
            ("^=",      new Operator(Op.BitXorAssign)),
            ("&&",      new Operator(Op.LogicAnd)),
            ("||",      new Operator(Op.LogicOr)),
            ("!",       new Operator(Op.LogicNot)),
            ("=",       new Operator(Op.Assign)),
            ("==",      new Operator(Op.Equal)),
            ("!=",      new Operator(Op.NotEqual)),
            (">",       new Operator(Op.Greater)),
            (">=",      new Operator(Op.GreaterOrEqual)),
            ("<",       new Operator(Op.LessThan)),
            ("<=",      new Operator(Op.LessOrEqual)),
            ("=>",      new Operator(Op.FatArrow)),
            ("..",      new Operator(Op.Range)),
            (",",       new ScopeControl(ScopeType.Inline,     ScopeDirection.Pop)),
            (";",       new ScopeControl(ScopeType.Statement,  ScopeDirection.Pop)),
            ("(",       new ScopeControl(ScopeType.Expression, ScopeDirection.Push)),
            (")",       new ScopeControl(ScopeType.Expression, ScopeDirection.Pop)),
            ("{",       new ScopeControl(ScopeType.Scope,      ScopeDirection.Push)),
            ("}",       new ScopeControl(ScopeType.Scope,      ScopeDirection.Pop)),
            ("[",       new ScopeControl(ScopeType.Subscript,  ScopeDirection.Push)),
            ("]",       new ScopeControl(ScopeType.Subscript,  ScopeDirection.Pop)),
        ],
        // language=regex
        numericLiteral: (@"[0-9]+", (word) => new NumLiteral(short.Parse(word))),
        // language=regex
        variableName: (@"[a-zA-Z_][a-zA-Z_0-9]*", (word) => new Variable(word)),
        // language=regex
        commentPattern: @"//.*?\n|/\*.*?\*/",
        isWhitespaceSensitive: false
    );
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
    static string FormatScope(ScopeType tag, string inner = "", bool isColored = true)
    {
        var (syntax, open, close) = tag switch {
            ScopeType.Expression
                => (Syntax.BracketGold, "(", ")"),
            ScopeType.Inline
                => (Syntax.RegexPaleYellow, "#(", ")"),
            ScopeType.Subscript
                => (Syntax.BracketPink, "[", "]"),
            ScopeType.Statement
                => (Syntax.BracketBlue, ":[", "];"),
            ScopeType.Scope
                => (Syntax.BracketPink, "{", "}"),

            _ => throw new NotImplementedException(),
        };
        return (isColored ? open.Highlight(syntax) : open) + inner + (isColored ? close.Highlight(syntax) : close);
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
        public string ToRich()
            => RichUnion(("scope", Syntax.Constant), FormatScope(tag,
                dir switch {
                    ScopeDirection.Push => "+",
                    ScopeDirection.Pop => "-",
                    _ => throw new NotImplementedException(),
                }
            ));
    }
    #endregion

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
    class TokenTree : IRichDisplay
    {
        public interface INode : IRichDisplay { }

        public interface IExprItem : INode { }

        public class Atom(IToken token) : INode, IExprItem
        {
            public readonly IToken token = token;
            public override string ToString() => $"{token}";
            public string ToRich() => token.ToRich();
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
            public abstract void Add(INode what);

            protected string InnerString()
            {
                string inner = string.Join("", Items.Select(x => $"\n{x},")).Replace("\n", "\n  ");
                return !string.IsNullOrEmpty(inner) ? inner + '\n' : string.Empty;
            }
            public override string ToString()
                => FormatScope(Tag, InnerString(), false);

            protected string InnerRich()
            {
                string inner = string.Join("", Items.Select(x => $"\n{x.ToRich()},")).Replace("\n", "\n  ");
                return !string.IsNullOrEmpty(inner) ? inner + '\n' : string.Empty;
            }
            public string ToRich()
                => FormatScope(Tag, InnerRich());
        }

        public class SubExpr(ScopeType tag) : Layer, IExprItem
        {
            public override ScopeType Tag => tag;
            public override IEnumerable<INode> Items => items;
            public override void Add(INode what) => items.Add(TryCast<IExprItem>(what));
            public readonly List<IExprItem> items = [];
        }

        // A statement can contain anything
        public class Statement : Layer
        {
            public override ScopeType Tag => ScopeType.Statement;
            public override IEnumerable<INode> Items => items;
            public override void Add(INode what) => items.Add(what);
            public readonly List<INode> items = [];
            public string ToLineString()
            {
                string inner = string.Join(", ", Items.Select(x => x is Scope
                    ? FormatScope(ScopeType.Scope, "...".Highlight(Syntax.Comment))
                    : x.ToRich()
                ));
                return FormatScope(Tag, inner);
            }
        }

        public class Scope : Layer
        {
            public override ScopeType Tag => ScopeType.Scope;
            public override IEnumerable<INode> Items => items;
            public override void Add(INode what) => items.Add(TryCast<Statement>(what));
            public readonly List<Statement> items = [];
        }

        public readonly Scope globalScope = new();

        public override string ToString() => globalScope.ToString();
        public string ToRich() => globalScope.ToRich();

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

            private static string PathFormat<T, U>(IEnumerable<T> path, Func<T, (ScopeType, U)> splitter) =>
                string.Join("->", path.Select((x) => {
                    var (layer, inner) = splitter(x);
                    return FormatScope(layer, inner.ToString());
                }));

            private static string PathFormat<T>(IEnumerable<T> path, Func<T, ScopeType> splitter) =>
                PathFormat(path, x => (splitter(x), string.Empty));

            private static string PathFormat(IEnumerable<ScopeType> path) =>
                PathFormat(path, x => (x, string.Empty));

            private string StackPath =>
                PathFormat(scopeStack.AsEnumerable().Reverse(), (layer) => (layer.Item1.Tag, layer.Item2));

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
                GD.PrintRich($"{StackPath} += {token.ToRich()}");
                Atom newAtom = new(token);
                CurrentScope.Add(newAtom);
            }

            private void PushScopes(ScopeType[] tags)
            {
                foreach (var tag in tags)
                {
                    var newScope = Layer.MakeFrom(tag);
                    int i = CurrentScope.Items.Count(x => x is Layer);
                    CurrentScope.Add(newScope);
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
                GD.PrintRich(StackPath + " += " + PathFormat(path));
                PushScopes(path);
            }

            public void PopScope(ScopeType tag)
            {
                var path = ImpliedPath(tag);
                GD.PrintRich(StackPath + " -= " + PathFormat(path));
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
    class Asm(IEnumerable<Asm.IToken[]> lines) {
        public interface IToken : IRichDisplay {}

        public readonly struct Op(string op) : IToken {
            readonly string op = op;
            public override readonly string ToString() => op;
            public readonly string ToRich() => op.Highlight(Syntax.Keyword);
        }

        public readonly struct Lbl(string name) : IToken {
            readonly string name = name;
            public override readonly string ToString() => name;
            public readonly string ToRich() => name.Highlight(Syntax.Function);
        }

        public readonly struct Lit(short value) : IToken {
            readonly short value = value;
            public override readonly string ToString() => value.ToString();
            public readonly string ToRich() => value.Highlight(Syntax.NumLiteral);
        }

        public readonly struct Comment(string text) : IToken {
            readonly string text = ";" + text.Replace("\n", "\n;");
            public override readonly string ToString() => ""; // make it easier on the assembly parser by not putting the comments in to begin with
            public readonly string ToRich() => string.Join('\n', text.Split('\n').Select(line => line.Highlight(Syntax.Comment)));
        }

        public List<IToken[]> lines = [..lines];
        public override string ToString()
            => string.Join('\n', lines.Select(x => string.Join(' ', x.Select(y => y.ToString()))));
        public string ToRich()
            => string.Join('\n', lines.Select(x => string.Join(' ', x.Select(y => y.ToRich()))));
    }

    interface IConcoction {
        public Asm ToAsm(string uniqueIdentifier);
    }

    /// <summary>
    /// kw(If), ($cond1), {$then1},
    /// kw(ElseIf), ($cond2), {$then2},
    /// ...
    /// kw(Else), {$ifNone}
    /// </summary>
    class Conditional : IConcoction
    {
        readonly public List<(TokenTree.INode[] cond, TokenTree.Scope then)> condThen = [];
        public TokenTree.Scope ifNone = null;

        public Asm ToAsm(string uniqueIdentifier)
        {
            Asm parts = new(condThen.SelectMany(IEnumerable<Asm.IToken[]>(item, n) => [
                [
                    new Asm.Comment($"todo:\n```\n{string.Join(',', item.cond.Select(x => x.ToRich()))}\n```"),
                ],
                [
                    new Asm.Op("jz"),
                    new Asm.Lbl($".else_{uniqueIdentifier}_{n}"),
                    new Asm.Comment($"if the line above evaluates to false (0), skip to condition {n+1}"),
                ],
                [
                    new Asm.Comment($"todo:\n```\n{item.then.ToRich()}\n```"),
                ],
                [
                    new Asm.Op("jmp"),
                    new Asm.Lbl($".finally_{uniqueIdentifier}"),
                    new Asm.Comment("the condition has completed, so none of the other conditions need to be tried"),
                ],
                [
                    new Asm.Lbl($".else_{uniqueIdentifier}_{n}:"),
                    new Asm.Comment($"condition {n+1}"),
                ],
            ]));
            if (ifNone is not null) {
                parts.lines.Add([new Asm.Comment($"todo:\n```\n{ifNone.ToRich()}\n```")]);
            }
            parts.lines.Add([
                new Asm.Lbl($".finally_{uniqueIdentifier}:"),
                new Asm.Comment("continue with the rest of the program after completing *any one* of the conditions"),
            ]);
            return parts;
        }
    }

    static T ExtractNext<T>(ref IEnumerable<T> iter)
    {
        var item = iter.FirstOrDefault();
        iter = iter.Skip(1);
        return item;
    }
    // static IEnumerable<T> Extract<T>(ref IEnumerable<T> iter, int n)
    // {
    //     var subset = iter.Take(n);
    //     iter = iter.Skip(n);
    //     return subset;
    // }
    static IEnumerable<T> ExtractWhile<T>(ref IEnumerable<T> iter, Func<T, bool> pred)
    {
        var subset = iter.TakeWhile(pred);
        iter = iter.Skip(subset.Count());
        return subset;
    }

    private static IEnumerable<IConcoction> Concoct(TokenTree.SubExpr expr)
    {
        throw new NotImplementedException();
    }

    private static IEnumerable<IConcoction> Concoct(TokenTree.Scope scope)
    {
        static IEnumerable<TokenTree.INode> ExtractCondition(ref IEnumerable<TokenTree.INode> iter)
        {
            var cond = ExtractWhile(ref iter, x => x is not TokenTree.Scope);
            if (cond.Any()) return cond;
            throw new SyntaxErrorException("If statement must ask something");
        }
        static TokenTree.Scope ExtractScope(ref IEnumerable<TokenTree.INode> iter)
        {
            if (ExtractNext(ref iter) is TokenTree.Scope then) return then;
            throw new SyntaxErrorException("If statement must do something");
        }

        List<IConcoction> items = [];
        foreach (var statement in scope.items)
        {
            GD.Print(); // gap
            GD.PrintRich(statement.ToLineString());
            IEnumerable<TokenTree.INode> iter = statement.items;
            switch (ExtractNext(ref iter))
            {
                case TokenTree.Atom { token: Keyword { kw: var kw } }:
                    switch (kw) {
                        case Kw.If:
                            {
                                GD.Print("If statement");
                                var cond = ExtractCondition(ref iter);
                                GD.PrintRich($"{cond.Count()} token condition: [{string.Join(", ",cond.Select(x=>x.ToRich()))}]");
                                var then = ExtractScope(ref iter);
                                GD.PrintRich($"if true: {then.ToRich()}");
                                items.Add(new Conditional() {
                                    condThen = { ([..cond], then) }
                                });
                            }
                            break;

                        case Kw.ElseIf:
                            {
                                GD.Print("ElseIf statement");
                                if (items.LastOrDefault() is Conditional conditional)
                                {
                                    var cond = ExtractCondition(ref iter);
                                    GD.PrintRich($"{cond.Count()} token condition: [{string.Join(", ",cond.Select(x=>x.ToRich()))}]");
                                    var then = ExtractScope(ref iter);
                                    GD.PrintRich($"if true: {then.ToRich()}");
                                    conditional.condThen.Add(([..cond], then));
                                }
                                else throw new SyntaxErrorException("'else' cannot start a statement");
                            }
                            break;

                        case Kw.Else:
                            {
                                GD.Print("Else statement");
                                if (items.LastOrDefault() is Conditional conditional)
                                {
                                    var then = ExtractScope(ref iter);
                                    GD.PrintRich($"else: {then.ToRich()}");
                                    conditional.ifNone = then;
                                }
                                else throw new SyntaxErrorException("'else' cannot start a statement");
                            }
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
            var tokens = tokenizer
                .Tokenize(code)
                .Select(x => {
                    var (word, token) = x;
                    var tkn = token is not null ? token.ToRich() : "[err]".Highlight(Syntax.Error);
                    GD.PrintRich($"\"{word}\"".Highlight(Syntax.StrLiteral) + " => " + tkn);
                    if (token is null) throw new SyntaxErrorException($"Unknown word: \"{word}\"");
                    return token;
                })
                .ToArray();

            GD.PrintRich("Building token tree...");
            var tree = Parse(tokens);
            GD.PrintRich($"Generated token tree:\n{tree.ToRich()}\n");

            GD.PrintRich("Concocting...");
            var concoction = Concoct(tree.globalScope);
            var asms = concoction.Select(x => x.ToAsm(x.GetHashCode().ToString()));
            {
                var richLines = asms.Select(x => x.ToRich());
                var rich = string.Join('\n', richLines);
                GD.PrintRich($"Generated concoction:\n```\n{rich}\n```");
            }
            var asmLines = asms.Select(x => x.ToString());
            var asm = string.Join('\n', asmLines);
            return ROM.Parse(asm);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Emulator compile error: {e.Message}\nNo ROM was generated");
            return null;
        }
    }
    #endregion

//     #region Example ROM
//     public static readonly ROM ExampleSy = Compile(@"
// let a = 5;
// if a == 3 {
//     b = 7
// } else if a == 6 {
//     b = 2
// } else {
//     b = 8
// }
// const NUM_LOOPS = 4;
// for (i in 0..NUM_LOOPS /* loop from 0 to NUM_LOOPS (i.e. 4) */) {
//     // do nothing
// }
// explode();
// ");
//     #endregion
}

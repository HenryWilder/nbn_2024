using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;

public static class NadeSy
{
    interface IToken {
        public static IToken ParseWord(string word) => word switch {
            _ when char.IsDigit(word[0])
                => new NumLiteral(short.Parse(word)),
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

    public enum Kw
    {
        If,
        Else,
        For,
        In,
        While,
        Let,
        Const,
    }
    class Keyword(Kw value) : IToken
    {
        public Kw value = value;
        public override string ToString() => $"kw({value})";
    }

    class Variable(string name) : IToken
    {
        public string name = name;
        public override string ToString() => $"var({name})";
    }

    class NumLiteral(short value) : IToken
    {
        public short value = value;
        public override string ToString() => $"num({value})";
    }

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
        public override string ToString() => $"op({op})";
    }

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
        public override string ToString() => $"scope({tag}.{dir})";
    }

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

            protected T TryCast<T>(INode what) where T: INode
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

            public override string ToString()
                => $"{Tag}\n[{InnerString()}]";
        }

        public class SubExpr(ScopeType tag) : Layer, IExprItem
        {
            public override ScopeType Tag => tag;
            public override IEnumerable<INode> Items => items;
            public override void Push(INode what) => items.Add(TryCast<IExprItem>(what));
            public readonly List<IExprItem> items = [];
        }

        // A statement can contain anything
        public class Statement : Layer
        {
            public override ScopeType Tag => ScopeType.Statement;
            public override IEnumerable<INode> Items => items;
            public override void Push(INode what) => items.Add(what);
            public readonly List<INode> items = [];
        }

        public class Scope : Layer
        {
            public override ScopeType Tag => ScopeType.Scope;
            public override IEnumerable<INode> Items => items;
            public override void Push(INode what) => items.Add(TryCast<Statement>(what));
            public readonly List<Statement> items = [];

            public override string ToString()
                => $"{{{InnerString()}}}";
        }

        public readonly Scope globalScope = new();

        public override string ToString() => globalScope.ToString();

        public class Builder {
            public Builder()
            {
                tree = new();
                scopeStack = new();
                scopeStack.Push((tree.globalScope, 0));
            }

            readonly TokenTree tree;
            readonly Stack<(Layer, int)> scopeStack;

            public Layer CurrentScope => scopeStack.First().Item1;

            private string StackPath => string.Join('.', scopeStack.AsEnumerable().Reverse().Select(layer => $"{layer.Item1.Tag}{layer.Item2}"));

            private static ScopeType[] ImpliedPath(ScopeType tag)
                => tag switch {
                    ScopeType.Scope
                        => [tag, ScopeType.Statement],

                    ScopeType.Expression or ScopeType.Subscript
                        => [tag, ScopeType.Inline],

                    _ => [tag],
                };

            public void PushAtom(IToken token)
            {
                GD.Print($"{StackPath} += {token}");
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

            public void PushScope(ScopeType tag)
            {
                GD.Print($"{StackPath} += {tag}");
                PushScopes(ImpliedPath(tag));
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
                if (tags.First() is ScopeType.Scope && CurrentScope.Tag == ScopeType.Statement) {
                    PopScope(ScopeType.Statement);
                }
            }

            public void PopScope(ScopeType tag)
            {
                var path = ImpliedPath(tag);
                GD.Print($"{StackPath} -= {string.Join('.',path)}");
                PopScopes(path);
            }

            public void PushToken(IToken token)
            {
                var missing = ImpliedPath(CurrentScope.Tag).Skip(1).ToArray();
                if (missing.Length > 0) PushScopes(missing);
                if (token is ScopeControl { tag: var tag, dir: var dir })
                {
                    switch (dir) {
                        case ScopeDirection.Push: PushScope(tag); break;
                        case ScopeDirection.Pop:  PopScope(tag);  break;
                    }
                }
                else
                {
                    PushAtom(token);
                }
            }

            public TokenTree Build() => tree;
        }
    }

    private static readonly Regex rxComments = new(
        @"//.*?\n|/\*.*?\*/",
        RegexOptions.Compiled);
    private static readonly Regex rxExcessSpaces = new(
        @"\s{2,}",
        RegexOptions.Compiled);

    // language=regex
    private static readonly string RX_KEYWORD_STR = string.Join('|', Enum.GetNames<Kw>());

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
        @$"\b(?:{RX_KEYWORD_STR}|{RX_VARNAME_STR}|{RX_NUM_LITERAL_STR})\b|{RX_SCOPING_STR}|{RX_OPERATOR_STR}",
        RegexOptions.Compiled);

    public static ROM Compile(string code)
    {
        try
        {
            GD.Print($"Compiling source code:\n```\n{code}\n```");
            code = rxComments.Replace(code, "");
            code = code.Replace('\n', ' ');
            code = rxExcessSpaces.Replace(code, " ");
            GD.Print("Tokenizing...");
            var tokens = rxTokenize
                .Matches(code)
                .Select(IToken (match, i) =>
                {
                    string word = match.Value;
                    try
                    {
                        var token = IToken.ParseWord(word);
                        GD.Print($"word {i}: \"{word}\" => {token}");
                        return token;
                    }
                    catch
                    {
                        GD.Print($"word {i}: [err]");
                        throw;
                    }
                })
                .ToArray();

            GD.Print("Building token tree...");
            TokenTree.Builder builder = new();
            foreach (var token in tokens)
            {
                GD.Print($"Token: {token}");
                builder.PushToken(token);
            }
            var tree = builder.Build();
            GD.Print($"Generated token tree:\n{tree}\n");

            GD.Print("Identifying patterns...");
            foreach (var statement in tree.globalScope.items) {
                GD.Print(statement);
            }
            GD.Print($"Generated patterns: [missing]");

            throw new NotImplementedException("todo");
            // return ROM.Parse(";todo");
        }
        catch (Exception e)
        {
            GD.PrintErr($"Emulator compile error: {e.Message}\nNo ROM was generated");
            return null;
        }
    }

    public static readonly ROM ExampleSy = Compile(@"
const NUM_LOOPS = 4;
for i in 0..NUM_LOOPS /* loop from 0 to N (4) */ {
    // do nothing
}
explode();
");
}

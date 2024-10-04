
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;

public readonly struct Tokenizer<Token>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="reserved"> NOT regex </param>
    /// <param name="numericLiteral"> regex </param>
    /// <param name="variableName"> regex </param>
    /// <param name="commentPattern"> regex </param>
    /// <param name="isWhitespaceSensitive">if false: removes newlines and truncates spaces.</param>
    public Tokenizer(
        IEnumerable<(string str, Token token)> reserved,
        (string pattern, Func<string, Token> converter) numericLiteral,
        (string pattern, Func<string, Token> converter) variableName,
        string commentPattern,
        bool isWhitespaceSensitive
    )
    {
        // smaller tokens have a chance of being contained inside larger tokens and disassembling them
        reserved = [..reserved.OrderByDescending(x => x.str.Length)];
        rxWord = new(
            @$"\b(?:{string.Join('|',reserved.Select(x=>x.str))}|{variableName.pattern})\b|{numericLiteral.pattern}", 
            RegexOptions.Compiled);
        reservedLookup = reserved.ToDictionary(x => x.str, x => x.token);
        rxIsNumLiteral = new(numericLiteral.pattern, RegexOptions.Compiled);
        parseNumLiteral = numericLiteral.converter;
        rxIsVariableName = new(variableName.pattern, RegexOptions.Compiled);
        parseVariableName = variableName.converter;
        rxComment = new (commentPattern, RegexOptions.Compiled);
        this.isWhitespaceSensitive = isWhitespaceSensitive;
    }

    private readonly Regex rxWord;
    private readonly Regex rxComment;
    private readonly Dictionary<string, Token> reservedLookup;
    private readonly Regex rxIsNumLiteral;
    private readonly Func<string, Token> parseNumLiteral;
    private readonly Regex rxIsVariableName;
    private readonly Func<string, Token> parseVariableName;
    private readonly bool isWhitespaceSensitive;
    private static readonly Regex rxExcessSpaces = new(@" {2,}|\t+", RegexOptions.Compiled);

    public readonly IEnumerable<(string, Token)> Tokenize(string code)
    {
        var self = this;
        code = rxComment.Replace(code, "");
        if (!isWhitespaceSensitive)
        {
            code = code.Replace('\n', ' ');
            code = rxExcessSpaces.Replace(code, " ");
        }
        return rxWord
            .Matches(code)
            .Select((match) =>
            {
                string word = match.Value;
                if (self.reservedLookup.TryGetValue(word, out var tkn))
                {
                    return (word, tkn);
                }
                if (self.rxIsNumLiteral.IsMatch(word))
                {
                    return (word, self.parseNumLiteral(word));
                }
                else if (self.rxIsVariableName.IsMatch(word))
                {
                    return (word, self.parseVariableName(word));
                }
                else return (word, default);
            });
    }
}

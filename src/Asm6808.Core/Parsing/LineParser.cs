using System.Text.RegularExpressions;

namespace Asm6808.Core.Parsing;

public static partial class LineParser
{
    private static readonly HashSet<string> Directives =
    [
        "END",
        "EQU",
        "FCB",
        "FCC",
        "FDB",
        "ORG",
        "RMB",
        "SET"
    ];

    private static readonly HashSet<string> KnownMnemonics =
    [
        "ABA",
        "ADDA",
        "ANDA",
        "ASLA",
        "BCC",
        "BEQ",
        "BHI",
        "BLS",
        "BNE",
        "BRA",
        "BSR",
        "CLI",
        "CLR",
        "CLRA",
        "CLRB",
        "CMPA",
        "DEX",
        "EQU",
        "INCA",
        "INX",
        "JMP",
        "JSR",
        "LDAA",
        "LDAB",
        "LDX",
        "LSRA",
        "ORAA",
        "PSHA",
        "PULA",
        "RTS",
        "SET",
        "SEI",
        "STAA",
        "STAB",
        "STX",
        "SUBA",
        "SWI",
        "TAB",
        "TBA",
        "TSTA",
        "TSTB"
    ];

    public static ParsedLine? Parse(string rawLine, int lineNumber)
    {
        if (rawLine is null)
        {
            throw new ArgumentNullException(nameof(rawLine));
        }

        var codePart = StripComment(rawLine).Trim();
        if (string.IsNullOrWhiteSpace(codePart))
        {
            return null;
        }

        if (codePart.StartsWith('*'))
        {
            return null;
        }

        var tokens = WhitespaceRegex().Split(codePart).Where(t => t.Length > 0).ToArray();
        if (tokens.Length == 0)
        {
            return null;
        }

        string? label = null;
        string mnemonic;
        string operandText = string.Empty;

        if (tokens[0].EndsWith(':'))
        {
            label = tokens[0][..^1];
            if (tokens.Length < 2)
            {
                mnemonic = string.Empty;
                return new ParsedLine(
                    LineNumber: lineNumber,
                    RawText: rawLine,
                    Label: label,
                    Mnemonic: mnemonic,
                    OperandText: string.Empty);
            }

            mnemonic = tokens[1].ToUpperInvariant();
            operandText = RemainingTokenText(codePart, tokens[0], tokens[1]);
        }
        else if (IsKnownMnemonicOrDirective(tokens[0]))
        {
            mnemonic = tokens[0].ToUpperInvariant();
            operandText = RemainingTokenText(codePart, tokens[0]);
        }
        else if (tokens.Length >= 2 && IsKnownMnemonicOrDirective(tokens[1]))
        {
            label = tokens[0];
            mnemonic = tokens[1].ToUpperInvariant();
            operandText = RemainingTokenText(codePart, tokens[0], tokens[1]);
        }
        else if (tokens.Length == 1)
        {
            return new ParsedLine(
                LineNumber: lineNumber,
                RawText: rawLine,
                Label: tokens[0],
                Mnemonic: string.Empty,
                OperandText: string.Empty);
        }
        else
        {
            mnemonic = tokens[0].ToUpperInvariant();
            operandText = RemainingTokenText(codePart, tokens[0]);
        }

        return new ParsedLine(
            LineNumber: lineNumber,
            RawText: rawLine,
            Label: label,
            Mnemonic: mnemonic,
            OperandText: operandText.Trim());
    }

    private static string StripComment(string rawLine)
    {
        var commentIndex = rawLine.IndexOf(';');
        return commentIndex >= 0 ? rawLine[..commentIndex] : rawLine;
    }

    private static string RemainingTokenText(string source, params string[] consumedTokens)
    {
        var remaining = source;
        foreach (var token in consumedTokens)
        {
            var index = remaining.IndexOf(token, StringComparison.Ordinal);
            if (index < 0)
            {
                return string.Empty;
            }

            remaining = remaining[(index + token.Length)..];
        }

        return remaining;
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();

    private static bool IsKnownMnemonicOrDirective(string token)
    {
        var upper = token.ToUpperInvariant();
        return KnownMnemonics.Contains(upper) || Directives.Contains(upper);
    }
}


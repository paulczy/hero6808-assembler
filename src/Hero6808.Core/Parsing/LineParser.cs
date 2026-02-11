using System.Text.RegularExpressions;

namespace Hero6808.Core.Parsing;

public static partial class LineParser
{
    private static readonly HashSet<string> Directives =
    [
        "ALIGN",
        "ASC",
        "BSS",
        "CLIST",
        "CODE",
        "CPU",
        "DB",
        "DUMMY",
        "DS",
        "DW",
        "END",
        "ENDC",
        "ENDIF",
        "ENDM",
        "EQU",
        "EXITM",
        "FCB",
        "FCC",
        "FDB",
        "IF",
        "ILIST",
        "INCLUDE",
        "LIST",
        "MACRO",
        "MLIST",
        "NAM",
        "ORG",
        "OUTPUT",
        "PAGE",
        "RMB",
        "SET",
        "TITLE"
    ];

    private static readonly HashSet<string> KnownMnemonics =
    [
        "ABA",
        "ADCA",
        "ADCB",
        "ADDA",
        "ADDB",
        "ANDA",
        "ANDB",
        "ASL",
        "ASLA",
        "ASLB",
        "ASR",
        "ASRA",
        "ASRB",
        "BCC",
        "BCS",
        "BEQ",
        "BGE",
        "BGT",
        "BITA",
        "BITB",
        "BHI",
        "BLE",
        "BLS",
        "BLT",
        "BMI",
        "BNE",
        "BPL",
        "BRA",
        "BSR",
        "BVC",
        "BVS",
        "CBA",
        "CLC",
        "CLI",
        "CLR",
        "CLRA",
        "CLRB",
        "CLV",
        "CMPA",
        "CMPB",
        "COM",
        "COMA",
        "COMB",
        "CPX",
        "DAA",
        "DECA",
        "DEC",
        "DECB",
        "DES",
        "DEX",
        "EORA",
        "EORB",
        "EQU",
        "INC",
        "INCA",
        "INCB",
        "INS",
        "INX",
        "JMP",
        "JSR",
        "LDA",
        "LDAA",
        "LDB",
        "LDAB",
        "LDS",
        "LDX",
        "LSR",
        "LSRA",
        "LSRB",
        "NEG",
        "NEGA",
        "NEGB",
        "NOP",
        "ORA",
        "ORAA",
        "ORAB",
        "PSHA",
        "PSHB",
        "PSHX",
        "PULA",
        "PULB",
        "PULX",
        "ROL",
        "ROLA",
        "ROLB",
        "ROR",
        "RORA",
        "RORB",
        "RTI",
        "RTS",
        "SBA",
        "SBCA",
        "SBCB",
        "SEC",
        "SET",
        "SEI",
        "SEV",
        "STA",
        "STAA",
        "STB",
        "STAB",
        "STS",
        "STX",
        "SUBA",
        "SUBB",
        "SWI",
        "TAB",
        "TAP",
        "TBA",
        "TPA",
        "TST",
        "TSTA",
        "TSTB",
        "TSX",
        "TXS",
        "WAI"
    ];

    public static ParsedLine? Parse(string rawLine, int lineNumber)
    {
        if (rawLine is null)
        {
            throw new ArgumentNullException(nameof(rawLine));
        }

        var stripped = StripComment(rawLine);
        var hasLeadingWhitespace = stripped.Length > 0 && char.IsWhiteSpace(stripped[0]);
        var codePart = stripped.Trim();
        if (string.IsNullOrWhiteSpace(codePart))
        {
            return null;
        }

        if (codePart.StartsWith('*'))
        {
            var remainder = codePart[1..].TrimStart();
            if (remainder.StartsWith("=", StringComparison.Ordinal))
            {
                return new ParsedLine(
                    LineNumber: lineNumber,
                    RawText: rawLine,
                    Label: null,
                    Mnemonic: "ORG",
                    OperandText: remainder[1..].Trim());
            }

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
        else if (tokens.Length >= 2 && tokens[1] == "=")
        {
            label = tokens[0];
            mnemonic = "SET";
            operandText = RemainingTokenText(codePart, tokens[0], tokens[1]);
        }
        else if (tokens.Length >= 2 && IsKnownMnemonicOrDirective(tokens[1]) &&
                 (!IsKnownMnemonicOrDirective(tokens[0]) || !hasLeadingWhitespace))
        {
            label = tokens[0];
            mnemonic = tokens[1].ToUpperInvariant();
            operandText = RemainingTokenText(codePart, tokens[0], tokens[1]);
        }
        else if (IsKnownMnemonicOrDirective(tokens[0]))
        {
            mnemonic = tokens[0].ToUpperInvariant();
            operandText = RemainingTokenText(codePart, tokens[0]);
        }
        else if (tokens.Length == 1)
        {
            // Preserve "label-only" lines, but avoid silently accepting indented unknown
            // opcodes (e.g., "    DECA") as labels.
            if (hasLeadingWhitespace)
            {
                mnemonic = tokens[0].ToUpperInvariant();
                return new ParsedLine(
                    LineNumber: lineNumber,
                    RawText: rawLine,
                    Label: null,
                    Mnemonic: mnemonic,
                    OperandText: string.Empty);
            }

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
        var inSingleQuote = false;
        var inDoubleQuote = false;

        for (var i = 0; i < rawLine.Length; i++)
        {
            var ch = rawLine[i];
            if (ch == '\'' && !inDoubleQuote)
            {
                if (inSingleQuote)
                {
                    inSingleQuote = false;
                    continue;
                }

                // Quoted literal like 'A' or ' '.
                if (i + 2 < rawLine.Length && rawLine[i + 2] == '\'')
                {
                    inSingleQuote = true;
                    continue;
                }

                // Motorola prefix-char form like 'G: consume next char as data.
                if (i + 1 < rawLine.Length)
                {
                    i++;
                }

                continue;
            }

            if (ch == '"' && !inSingleQuote)
            {
                inDoubleQuote = !inDoubleQuote;
                continue;
            }

            if (ch == ';' && !inSingleQuote && !inDoubleQuote)
            {
                return rawLine[..i];
            }
        }

        return rawLine;
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



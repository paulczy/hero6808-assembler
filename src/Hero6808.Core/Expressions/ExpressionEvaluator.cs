using System.Globalization;
using System.Text.RegularExpressions;

namespace Hero6808.Core.Expressions;

public static class ExpressionEvaluator
{
    public static bool TryEvaluate(
        string expression,
        IReadOnlyDictionary<string, int> symbols,
        out int value,
        out string? unresolvedSymbol)
    {
        return TryEvaluate(expression, symbols, out value, out unresolvedSymbol, currentAddress: null);
    }

    public static bool TryEvaluate(
        string expression,
        IReadOnlyDictionary<string, int> symbols,
        out int value,
        out string? unresolvedSymbol,
        int? currentAddress)
    {
        if (expression is null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        unresolvedSymbol = null;
        value = 0;

        var source = expression.Trim();
        if (source.Length == 0)
        {
            return false;
        }

        var parser = new Parser(source, symbols, currentAddress);
        if (!parser.TryParse(out value, out unresolvedSymbol))
        {
            return false;
        }

        return true;
    }

    private static bool TryParseHex(string term, out int value)
    {
        if (term.Length > 1 && term[0] == '$')
        {
            return int.TryParse(term[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        value = 0;
        return false;
    }

    private static bool TryParseDecimal(string term, out int value)
    {
        return int.TryParse(term, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseBinary(string term, out int value)
    {
        value = 0;
        if (term.Length <= 1 || term[0] != '%')
        {
            return false;
        }

        foreach (var ch in term[1..])
        {
            if (ch is not ('0' or '1'))
            {
                value = 0;
                return false;
            }

            value = (value << 1) + (ch - '0');
        }

        return true;
    }

    private static bool TryParseCharLiteral(string term, out int value)
    {
        // Support assembler-style prefix form: 'A
        if (term.Length == 2 && term[0] == '\'')
        {
            value = term[1];
            return true;
        }

        // Support quoted single-character forms: 'A' or "A".
        if (term.Length == 3 &&
            ((term[0] == '\'' && term[2] == '\'') || (term[0] == '"' && term[2] == '"')))
        {
            value = term[1];
            return true;
        }

        value = 0;
        return false;
    }

    private static bool IsSymbol(string term)
    {
        return Regex.IsMatch(term, "^[A-Za-z_.][A-Za-z0-9_.]*$");
    }

    private sealed class Parser(string expression, IReadOnlyDictionary<string, int> symbols, int? currentAddress)
    {
        private readonly string _source = expression;
        private readonly IReadOnlyDictionary<string, int> _symbols = symbols;
        private readonly int? _currentAddress = currentAddress;
        private int _index;
        private string? _unresolvedSymbol;

        public bool TryParse(out int value, out string? unresolvedSymbol)
        {
            _index = 0;
            _unresolvedSymbol = null;
            value = 0;

            if (!TryParseBitwiseOr(out value))
            {
                unresolvedSymbol = _unresolvedSymbol;
                return false;
            }

            SkipWhitespace();
            if (_index != _source.Length)
            {
                unresolvedSymbol = _unresolvedSymbol;
                return false;
            }

            unresolvedSymbol = null;
            return true;
        }

        private bool TryParseBitwiseOr(out int value)
        {
            if (!TryParseBitwiseXor(out value))
            {
                return false;
            }

            while (true)
            {
                SkipWhitespace();
                if (!Consume('|'))
                {
                    return true;
                }

                if (!TryParseBitwiseXor(out var right))
                {
                    return false;
                }

                value |= right;
            }
        }

        private bool TryParseBitwiseXor(out int value)
        {
            if (!TryParseBitwiseAnd(out value))
            {
                return false;
            }

            while (true)
            {
                SkipWhitespace();
                if (!Consume('^'))
                {
                    return true;
                }

                if (!TryParseBitwiseAnd(out var right))
                {
                    return false;
                }

                value ^= right;
            }
        }

        private bool TryParseBitwiseAnd(out int value)
        {
            if (!TryParseShift(out value))
            {
                return false;
            }

            while (true)
            {
                SkipWhitespace();
                if (!Consume('&'))
                {
                    return true;
                }

                if (!TryParseShift(out var right))
                {
                    return false;
                }

                value &= right;
            }
        }

        private bool TryParseShift(out int value)
        {
            if (!TryParseAddSub(out value))
            {
                return false;
            }

            while (true)
            {
                SkipWhitespace();
                if (Consume("<<"))
                {
                    if (!TryParseAddSub(out var right))
                    {
                        return false;
                    }

                    value <<= right;
                    continue;
                }

                if (Consume(">>"))
                {
                    if (!TryParseAddSub(out var right))
                    {
                        return false;
                    }

                    value >>= right;
                    continue;
                }

                return true;
            }
        }

        private bool TryParseAddSub(out int value)
        {
            if (!TryParseMulDiv(out value))
            {
                return false;
            }

            while (true)
            {
                SkipWhitespace();
                if (Consume('+'))
                {
                    if (!TryParseMulDiv(out var right))
                    {
                        return false;
                    }

                    value += right;
                    continue;
                }

                if (Consume('-'))
                {
                    if (!TryParseMulDiv(out var right))
                    {
                        return false;
                    }

                    value -= right;
                    continue;
                }

                return true;
            }
        }

        private bool TryParseMulDiv(out int value)
        {
            if (!TryParseUnary(out value))
            {
                return false;
            }

            while (true)
            {
                SkipWhitespace();
                if (Consume('*'))
                {
                    if (!TryParseUnary(out var right))
                    {
                        return false;
                    }

                    value *= right;
                    continue;
                }

                if (Consume('/'))
                {
                    if (!TryParseUnary(out var right) || right == 0)
                    {
                        return false;
                    }

                    value /= right;
                    continue;
                }

                return true;
            }
        }

        private bool TryParseUnary(out int value)
        {
            SkipWhitespace();

            if (Consume('#') || Consume('+'))
            {
                return TryParseUnary(out value);
            }

            if (Consume('-'))
            {
                if (!TryParseUnary(out value))
                {
                    return false;
                }

                value = -value;
                return true;
            }

            if (Consume('~'))
            {
                if (!TryParseUnary(out value))
                {
                    return false;
                }

                value = ~value;
                return true;
            }

            return TryParsePrimary(out value);
        }

        private bool TryParsePrimary(out int value)
        {
            SkipWhitespace();

            if (Consume('('))
            {
                if (!TryParseBitwiseOr(out value))
                {
                    return false;
                }

                SkipWhitespace();
                return Consume(')');
            }

            if (Consume('*'))
            {
                if (_currentAddress is null)
                {
                    _unresolvedSymbol = "*";
                    value = 0;
                    return false;
                }

                value = _currentAddress.Value;
                return true;
            }

            if (TryReadToken(out var token))
            {
                if (TryParseHex(token, out value) ||
                    TryParseBinary(token, out value) ||
                    TryParseDecimal(token, out value) ||
                    TryParseCharLiteral(token, out value) ||
                    TryParseHexSuffix(token, out value))
                {
                    return true;
                }

                if (IsSymbol(token))
                {
                    if (_symbols.TryGetValue(token, out value))
                    {
                        return true;
                    }

                    _unresolvedSymbol = token;
                }
            }

            value = 0;
            return false;
        }

        private bool TryReadToken(out string token)
        {
            SkipWhitespace();
            if (_index >= _source.Length)
            {
                token = string.Empty;
                return false;
            }

            if (_source[_index] is '\'' or '"')
            {
                var delimiter = _source[_index];
                var start = _index;
                _index++;
                while (_index < _source.Length && _source[_index] != delimiter)
                {
                    _index++;
                }

                if (_index < _source.Length && _source[_index] == delimiter)
                {
                    _index++;
                    token = _source[start.._index];
                    return token.Length > 0;
                }

                // Support Motorola prefix-char form like 'G (no trailing quote).
                token = _source[start.._index];
                return token.Length > 0;
            }

            var tokenStart = _index;
            while (_index < _source.Length)
            {
                var ch = _source[_index];
                if (char.IsWhiteSpace(ch) || IsOperatorChar(ch))
                {
                    break;
                }

                _index++;
            }

            token = _source[tokenStart.._index];
            return token.Length > 0;
        }

        private static bool IsOperatorChar(char ch)
        {
            return ch is '+' or '-' or '*' or '/' or '(' or ')' or '&' or '|' or '^' or '<' or '>' or '~' or '#';
        }

        private void SkipWhitespace()
        {
            while (_index < _source.Length && char.IsWhiteSpace(_source[_index]))
            {
                _index++;
            }
        }

        private bool Consume(char ch)
        {
            if (_index < _source.Length && _source[_index] == ch)
            {
                _index++;
                return true;
            }

            return false;
        }

        private bool Consume(string text)
        {
            if (_index + text.Length > _source.Length)
            {
                return false;
            }

            for (var i = 0; i < text.Length; i++)
            {
                if (_source[_index + i] != text[i])
                {
                    return false;
                }
            }

            _index += text.Length;
            return true;
        }

        private static bool TryParseHexSuffix(string term, out int value)
        {
            value = 0;
            if (term.Length < 2)
            {
                return false;
            }

            if (term[^1] is not ('H' or 'h'))
            {
                return false;
            }

            return int.TryParse(term[..^1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }
    }
}




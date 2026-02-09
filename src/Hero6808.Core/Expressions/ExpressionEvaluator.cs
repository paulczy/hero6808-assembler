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

        var normalized = source.Replace(" ", string.Empty);
        var sign = 1;
        var index = 0;

        while (index < normalized.Length)
        {
            var current = normalized[index];
            if (current == '+')
            {
                sign = 1;
                index++;
                continue;
            }

            if (current == '-')
            {
                sign = -1;
                index++;
                continue;
            }

            var start = index;
            while (index < normalized.Length && normalized[index] != '+' && normalized[index] != '-')
            {
                index++;
            }

            var term = normalized[start..index];
            if (!TryEvaluateTerm(term, symbols, out var termValue, out unresolvedSymbol))
            {
                return false;
            }

            value += sign * termValue;
            sign = 1;
        }

        return true;
    }

    private static bool TryEvaluateTerm(
        string term,
        IReadOnlyDictionary<string, int> symbols,
        out int value,
        out string? unresolvedSymbol)
    {
        unresolvedSymbol = null;
        value = 0;

        if (term.Length == 0)
        {
            return false;
        }

        if (term[0] == '#')
        {
            term = term[1..];
            if (term.Length == 0)
            {
                return false;
            }
        }

        if (TryParseHex(term, out value) || TryParseDecimal(term, out value) || TryParseCharLiteral(term, out value))
        {
            return true;
        }

        if (IsSymbol(term))
        {
            if (!symbols.TryGetValue(term, out value))
            {
                unresolvedSymbol = term;
                return false;
            }

            return true;
        }

        return false;
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

    private static bool TryParseCharLiteral(string term, out int value)
    {
        // Support assembler-style prefix form: 'A
        if (term.Length == 2 && term[0] == '\'')
        {
            value = term[1];
            return true;
        }

        value = 0;
        return false;
    }

    private static bool IsSymbol(string term)
    {
        return Regex.IsMatch(term, "^[A-Za-z_][A-Za-z0-9_]*$");
    }
}



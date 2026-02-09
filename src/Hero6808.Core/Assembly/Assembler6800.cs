using Hero6808.Core.Expressions;
using Hero6808.Core.Parsing;
using Hero6808.Core.SRecords;
using System.Diagnostics.CodeAnalysis;

namespace Hero6808.Core.Assembly;

public sealed class Assembler6800
{
    private static readonly HashSet<string> Directives =
    [
        "EQU",
        "SET",
        "ORG",
        "FCB",
        "FCC",
        "FDB",
        "RMB",
        "END"
    ];

    public AssemblyResult Assemble(IEnumerable<string> sourceLines, string sourceName = "<input>")
    {
        var parsedLines = ParseLines(sourceLines);
        var symbols = BuildSymbolTable(parsedLines, sourceName);
        var segments = EmitSegments(parsedLines, symbols, sourceName);
        return new AssemblyResult(segments, symbols);
    }

    public IReadOnlyList<string> AssembleToS19Records(
        IEnumerable<string> sourceLines,
        int dataBytesPerRecord = 32,
        ushort executionAddress = 0x0000,
        string sourceName = "<input>")
    {
        var result = Assemble(sourceLines, sourceName);
        return S19Writer.WriteRecords(result.Segments, dataBytesPerRecord, executionAddress);
    }

    private static List<ParsedLine> ParseLines(IEnumerable<string> sourceLines)
    {
        var parsedLines = new List<ParsedLine>();
        var lineNumber = 0;
        foreach (var raw in sourceLines)
        {
            lineNumber++;
            var parsed = LineParser.Parse(raw, lineNumber);
            if (parsed is not null)
            {
                parsedLines.Add(parsed);
            }
        }

        return parsedLines;
    }

    private static Dictionary<string, int> BuildSymbolTable(IReadOnlyList<ParsedLine> lines, string sourceName)
    {
        var symbols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var pc = 0;

        foreach (var line in lines)
        {
            var mnemonic = line.Mnemonic;
            if (line.Label is not null && mnemonic is not "EQU" and not "SET")
            {
                symbols[line.Label] = pc;
            }

            if (mnemonic.Length == 0)
            {
                continue;
            }

            if (mnemonic is "EQU" or "SET")
            {
                if (line.Label is null)
                {
                    ThrowDiagnostic(sourceName, line.LineNumber, $"{mnemonic} requires a label.");
                }

                if (!ExpressionEvaluator.TryEvaluate(line.OperandText, symbols, out var equValue, out var unresolved))
                {
                    ThrowDiagnostic(
                        sourceName,
                        line.LineNumber,
                        $"cannot evaluate {mnemonic} expression '{line.OperandText}' (unresolved: {unresolved ?? "unknown"}).");
                }

                symbols[line.Label] = equValue;
                continue;
            }

            if (mnemonic == "ORG")
            {
                pc = EvaluateExpressionOrThrow(line, symbols, sourceName);
                continue;
            }

            if (mnemonic == "FCB")
            {
                pc += SplitCsv(line.OperandText).Count;
                continue;
            }

            if (mnemonic == "FCC")
            {
                pc += ParseFccText(line.OperandText, line.LineNumber, sourceName).Length;
                continue;
            }

            if (mnemonic == "FDB")
            {
                pc += SplitCsv(line.OperandText).Count * 2;
                continue;
            }

            if (mnemonic == "RMB")
            {
                pc += EvaluateExpressionOrThrow(line, symbols, sourceName);
                continue;
            }

            if (mnemonic == "END")
            {
                break;
            }

            if (!OpcodeTable.IsKnownMnemonic(mnemonic))
            {
                ThrowDiagnostic(sourceName, line.LineNumber, $"unknown mnemonic '{mnemonic}'.");
            }

            var mode = ResolveAddressingMode(line, symbols);
            if (!OpcodeTable.TryGet(mnemonic, mode, out var encoding))
            {
                ThrowDiagnostic(sourceName, line.LineNumber, $"mnemonic '{mnemonic}' does not support {mode} mode.");
            }

            pc += 1 + encoding.OperandBytes;
        }

        return symbols;
    }

    private static List<AddressedBytes> EmitSegments(
        IReadOnlyList<ParsedLine> lines,
        IReadOnlyDictionary<string, int> symbols,
        string sourceName)
    {
        var segments = new List<AddressedBytes>();
        var currentData = new List<byte>();
        ushort? currentSegmentStart = null;
        var pc = 0;

        void FlushSegment()
        {
            if (currentSegmentStart is not null && currentData.Count > 0)
            {
                segments.Add(new AddressedBytes((ushort)currentSegmentStart.Value, currentData.ToArray()));
                currentData.Clear();
                currentSegmentStart = null;
            }
        }

        void EmitByte(byte value)
        {
            if (currentSegmentStart is null)
            {
                currentSegmentStart = (ushort)pc;
            }

            var expectedAddress = currentSegmentStart.Value + currentData.Count;
            if (expectedAddress != pc)
            {
                FlushSegment();
                currentSegmentStart = (ushort)pc;
            }

            currentData.Add(value);
            pc++;
        }

        foreach (var line in lines)
        {
            var mnemonic = line.Mnemonic;
            if (mnemonic.Length == 0)
            {
                continue;
            }

            if (mnemonic is "EQU" or "SET")
            {
                continue;
            }

            if (mnemonic == "ORG")
            {
                pc = EvaluateExpressionOrThrow(line, symbols, sourceName);
                continue;
            }

            if (mnemonic == "FCB")
            {
                foreach (var item in SplitCsv(line.OperandText))
                {
                    var value = EvaluateExpressionOrThrow(line with { OperandText = item }, symbols, sourceName);
                    EmitByte((byte)(value & 0xFF));
                }

                continue;
            }

            if (mnemonic == "FCC")
            {
                foreach (var ch in ParseFccText(line.OperandText, line.LineNumber, sourceName))
                {
                    EmitByte((byte)ch);
                }

                continue;
            }

            if (mnemonic == "FDB")
            {
                foreach (var item in SplitCsv(line.OperandText))
                {
                    var value = EvaluateExpressionOrThrow(line with { OperandText = item }, symbols, sourceName);
                    EmitByte((byte)((value >> 8) & 0xFF));
                    EmitByte((byte)(value & 0xFF));
                }

                continue;
            }

            if (mnemonic == "RMB")
            {
                pc += EvaluateExpressionOrThrow(line, symbols, sourceName);
                continue;
            }

            if (mnemonic == "END")
            {
                break;
            }

            var mode = ResolveAddressingMode(line, symbols);
            if (!OpcodeTable.TryGet(mnemonic, mode, out var encoding))
            {
                ThrowDiagnostic(sourceName, line.LineNumber, $"mnemonic '{mnemonic}' does not support {mode} mode.");
            }

            EmitByte(encoding.Opcode);
            EmitOperand(line, mode, encoding.OperandBytes, symbols, pc, EmitByte, sourceName);
        }

        FlushSegment();
        return segments;
    }

    private static void EmitOperand(
        ParsedLine line,
        AddressingMode mode,
        int operandBytes,
        IReadOnlyDictionary<string, int> symbols,
        int pcAfterOpcode,
        Action<byte> emitByte,
        string sourceName)
    {
        if (operandBytes == 0)
        {
            return;
        }

        if (mode == AddressingMode.Indexed)
        {
            var offset = ParseIndexedOffset(line, symbols, sourceName);
            emitByte((byte)(offset & 0xFF));
            return;
        }

        if (mode == AddressingMode.Relative)
        {
            var target = EvaluateExpressionOrThrow(line, symbols, sourceName);
            var nextInstruction = pcAfterOpcode + 1;
            var delta = target - nextInstruction;
            if (delta < -128 || delta > 127)
            {
                var message = line.Mnemonic.Equals("BSR", StringComparison.OrdinalIgnoreCase)
                    ? "branch target out of range for BSR; use JSR for far calls."
                    : "branch target out of range.";
                ThrowDiagnostic(sourceName, line.LineNumber, message);
            }

            emitByte((byte)(delta & 0xFF));
            return;
        }

        var value = EvaluateExpressionOrThrow(line, symbols, sourceName);
        if (operandBytes == 1)
        {
            emitByte((byte)(value & 0xFF));
            return;
        }

        emitByte((byte)((value >> 8) & 0xFF));
        emitByte((byte)(value & 0xFF));
    }

    private static int ParseIndexedOffset(ParsedLine line, IReadOnlyDictionary<string, int> symbols, string sourceName)
    {
        var parts = line.OperandText.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !parts[1].Equals("X", StringComparison.OrdinalIgnoreCase))
        {
            ThrowDiagnostic(sourceName, line.LineNumber, $"invalid indexed operand '{line.OperandText}'.");
        }

        if (string.IsNullOrWhiteSpace(parts[0]))
        {
            return 0;
        }

        var value = EvaluateExpressionOrThrow(line with { OperandText = parts[0] }, symbols, sourceName);
        if (value < 0 || value > 0xFF)
        {
            ThrowDiagnostic(sourceName, line.LineNumber, "indexed offset out of range 0..255.");
        }

        return value;
    }

    private static AddressingMode ResolveAddressingMode(ParsedLine line, IReadOnlyDictionary<string, int> symbols)
    {
        if (line.Mnemonic is
            "BCC" or "BCS" or "BEQ" or "BGE" or "BGT" or "BHI" or "BLE" or "BLS" or "BLT" or "BMI" or "BNE" or
            "BPL" or "BRA" or "BSR" or "BVC" or "BVS")
        {
            return AddressingMode.Relative;
        }

        var operand = line.OperandText.Trim();
        if (operand.Length == 0)
        {
            return AddressingMode.Inherent;
        }

        if (operand.StartsWith("#", StringComparison.Ordinal))
        {
            return AddressingMode.Immediate;
        }

        if (operand.Contains(',', StringComparison.Ordinal))
        {
            return AddressingMode.Indexed;
        }

        if (OpcodeTable.SupportsMode(line.Mnemonic, AddressingMode.Direct) &&
            TryResolveDirectValue(operand, symbols, out _))
        {
            return AddressingMode.Direct;
        }

        return AddressingMode.Extended;
    }

    private static bool IsDirectLiteral(string operand)
    {
        if (operand.StartsWith("$", StringComparison.Ordinal))
        {
            var hex = operand[1..];
            return hex.Length is 1 or 2 && int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out _);
        }

        return int.TryParse(operand, out var value) && value is >= 0 and <= 0xFF;
    }

    private static bool TryResolveDirectValue(string operand, IReadOnlyDictionary<string, int> symbols, out int value)
    {
        value = 0;
        if (IsDirectLiteral(operand))
        {
            if (operand.StartsWith("$", StringComparison.Ordinal))
            {
                return int.TryParse(operand[1..], System.Globalization.NumberStyles.HexNumber, null, out value);
            }

            return int.TryParse(operand, out value);
        }

        if (ExpressionEvaluator.TryEvaluate(operand, symbols, out var resolved, out _))
        {
            if (resolved is >= 0 and <= 0xFF)
            {
                value = resolved;
                return true;
            }
        }

        return false;
    }

    private static int EvaluateExpressionOrThrow(ParsedLine line, IReadOnlyDictionary<string, int> symbols, string sourceName)
    {
        if (!ExpressionEvaluator.TryEvaluate(line.OperandText, symbols, out var value, out var unresolved))
        {
            ThrowDiagnostic(
                sourceName,
                line.LineNumber,
                $"cannot evaluate expression '{line.OperandText}' (unresolved: {unresolved ?? "unknown"}).");
        }

        return value;
    }

    private static List<string> SplitCsv(string operandText)
    {
        return operandText
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    private static string ParseFccText(string operandText, int lineNumber, string sourceName)
    {
        var trimmed = operandText.Trim();
        if (trimmed.Length < 2)
        {
            ThrowDiagnostic(sourceName, lineNumber, "FCC requires a delimited string literal.");
        }

        var delimiter = trimmed[0];
        if (char.IsLetterOrDigit(delimiter) || char.IsWhiteSpace(delimiter))
        {
            ThrowDiagnostic(sourceName, lineNumber, "FCC requires a quoted or delimited string literal.");
        }

        if (trimmed[^1] != delimiter)
        {
            ThrowDiagnostic(sourceName, lineNumber, "FCC string delimiter must match at both ends.");
        }

        return trimmed[1..^1];
    }

    [DoesNotReturn]
    private static void ThrowDiagnostic(string sourceName, int lineNumber, string message)
    {
        throw new InvalidOperationException($"{sourceName}:{lineNumber}: {message}");
    }
}



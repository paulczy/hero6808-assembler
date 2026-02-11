using Hero6808.Core.Expressions;
using Hero6808.Core.Parsing;
using Hero6808.Core.SRecords;
using System.Diagnostics.CodeAnalysis;

namespace Hero6808.Core.Assembly;

public sealed class Assembler6800
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
        "ENDC",
        "ENDIF",
        "ENDM",
        "EQU",
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
        "TITLE",
        "END"
    ];

    private static readonly HashSet<string> LikelyMasmX86Tokens =
    [
        "ASSUME",
        "CODE",
        "DATA",
        "DB",
        "DD",
        "DQ",
        "DW",
        "DUP",
        "ENDP",
        "ENDS",
        "INT",
        "LEA",
        "MODEL",
        "OFFSET",
        "PROC",
        "PTR",
        "SEGMENT",
        "STACK"
    ];

    public AssemblyResult Assemble(IEnumerable<string> sourceLines, string sourceName = "<input>")
    {
        var parsedLines = ParseLines(sourceLines);
        var widenedBsrLines = ResolveFarBsrLines(parsedLines, sourceName);
        var symbols = BuildSymbolTable(parsedLines, sourceName, widenedBsrLines);
        var segments = EmitSegments(parsedLines, symbols, sourceName, widenedBsrLines);
        return new AssemblyResult(segments, symbols);
    }

    public IReadOnlyList<string> AssembleToS19Records(
        IEnumerable<string> sourceLines,
        int dataBytesPerRecord = 32,
        ushort executionAddress = 0x0000,
        string sourceName = "<input>")
    {
        var materializedLines = sourceLines as IReadOnlyList<string> ?? sourceLines.ToList();
        var result = Assemble(materializedLines, sourceName);
        var effectiveDataBytesPerRecord = ResolveDataBytesPerRecord(materializedLines, dataBytesPerRecord);
        return S19Writer.WriteRecords(result.Segments, effectiveDataBytesPerRecord, executionAddress);
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

    private static Dictionary<string, int> BuildSymbolTable(
        IReadOnlyList<ParsedLine> lines,
        string sourceName,
        ISet<int>? widenedBsrLines = null)
    {
        widenedBsrLines ??= new HashSet<int>();
        var symbols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var pc = 0;
        var conditionals = new Stack<(bool ParentActive, bool ConditionTrue, bool ElseSeen)>();
        var isActive = true;

        foreach (var line in lines)
        {
            var mnemonic = NormalizeMnemonic(line.Mnemonic);
            if (mnemonic == "IF")
            {
                var cond = false;
                if (isActive)
                {
                    cond = EvaluateConditionalExpressionOrThrow(line, symbols, pc, sourceName);
                }

                conditionals.Push((isActive, cond, false));
                isActive = isActive && cond;
                continue;
            }

            if (mnemonic == "ELSE")
            {
                if (conditionals.Count == 0)
                {
                    ThrowDiagnostic(sourceName, line.LineNumber, "ELSE without matching IF.");
                }

                var frame = conditionals.Pop();
                if (frame.ElseSeen)
                {
                    ThrowDiagnostic(sourceName, line.LineNumber, "duplicate ELSE for IF block.");
                }

                conditionals.Push((frame.ParentActive, frame.ConditionTrue, true));
                isActive = frame.ParentActive && !frame.ConditionTrue;
                continue;
            }

            if (mnemonic is "ENDC" or "ENDIF")
            {
                if (conditionals.Count == 0)
                {
                    ThrowDiagnostic(sourceName, line.LineNumber, "ENDC/ENDIF without matching IF.");
                }

                var frame = conditionals.Pop();
                isActive = frame.ParentActive;
                continue;
            }

            if (!isActive)
            {
                continue;
            }

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

                if (!ExpressionEvaluator.TryEvaluate(line.OperandText, symbols, out var equValue, out var unresolved, pc))
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
                pc = EvaluateExpressionOrThrow(line, symbols, pc, sourceName);
                continue;
            }

            if (mnemonic is "FCB" or "DB")
            {
                foreach (var item in SplitCsv(line.OperandText))
                {
                    if (TryParseDbStringLiteral(item, line.LineNumber, sourceName, out var text))
                    {
                        pc += text.Length;
                    }
                    else
                    {
                        pc += 1;
                    }
                }

                continue;
            }

            if (mnemonic == "ASC")
            {
                pc += ParseAscText(line.OperandText, line.LineNumber, sourceName).Length;
                continue;
            }

            if (mnemonic == "FCC")
            {
                pc += ParseFccText(line.OperandText, line.LineNumber, sourceName).Length;
                continue;
            }

            if (mnemonic is "FDB" or "DW")
            {
                pc += SplitCsv(line.OperandText).Count * 2;
                continue;
            }

            if (mnemonic == "RMB")
            {
                pc += EvaluateExpressionOrThrow(line, symbols, pc, sourceName);
                continue;
            }

            if (mnemonic == "DS")
            {
                var (count, _) = ParseDsOperands(line, symbols, pc, sourceName);
                pc += count;
                continue;
            }

            if (mnemonic == "END")
            {
                break;
            }

            if (IsCrAsmNoOpDirective(mnemonic))
            {
                continue;
            }

            if (mnemonic == "BSR" && widenedBsrLines.Contains(line.LineNumber))
            {
                pc += 3; // Relaxed to JSR extended.
                continue;
            }

            if (Directives.Contains(mnemonic))
            {
                ThrowDiagnostic(sourceName, line.LineNumber, $"directive '{mnemonic}' is recognized but not implemented yet.");
            }

            if (!OpcodeTable.IsKnownMnemonic(mnemonic))
            {
                if (LooksLikeMasmX86Dialect(line))
                {
                    ThrowDiagnostic(
                        sourceName,
                        line.LineNumber,
                        $"detected MASM/x86-style syntax near '{mnemonic}'. Hero6808 assembles Motorola 6800/6808 source.");
                }

                ThrowDiagnostic(sourceName, line.LineNumber, $"unknown mnemonic '{mnemonic}'.");
            }

            var normalizedLine = line with { Mnemonic = mnemonic };
            var mode = ResolveAddressingMode(normalizedLine, symbols);
            if (!OpcodeTable.TryGet(mnemonic, mode, out var encoding))
            {
                ThrowDiagnostic(sourceName, line.LineNumber, $"mnemonic '{mnemonic}' does not support {mode} mode.");
            }

            pc += 1 + encoding.OperandBytes;
        }

        if (conditionals.Count > 0)
        {
            ThrowDiagnostic(sourceName, lines[^1].LineNumber, "unterminated IF block (missing ENDC).");
        }

        return symbols;
    }

    private static List<AddressedBytes> EmitSegments(
        IReadOnlyList<ParsedLine> lines,
        IReadOnlyDictionary<string, int> symbols,
        string sourceName,
        ISet<int>? widenedBsrLines = null)
    {
        widenedBsrLines ??= new HashSet<int>();
        var segments = new List<AddressedBytes>();
        var currentData = new List<byte>();
        ushort? currentSegmentStart = null;
        var pc = 0;
        var conditionals = new Stack<(bool ParentActive, bool ConditionTrue, bool ElseSeen)>();
        var isActive = true;

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
            var mnemonic = NormalizeMnemonic(line.Mnemonic);
            if (mnemonic == "IF")
            {
                var cond = false;
                if (isActive)
                {
                    cond = EvaluateConditionalExpressionOrThrow(line, symbols, pc, sourceName);
                }

                conditionals.Push((isActive, cond, false));
                isActive = isActive && cond;
                continue;
            }

            if (mnemonic == "ELSE")
            {
                if (conditionals.Count == 0)
                {
                    ThrowDiagnostic(sourceName, line.LineNumber, "ELSE without matching IF.");
                }

                var frame = conditionals.Pop();
                if (frame.ElseSeen)
                {
                    ThrowDiagnostic(sourceName, line.LineNumber, "duplicate ELSE for IF block.");
                }

                conditionals.Push((frame.ParentActive, frame.ConditionTrue, true));
                isActive = frame.ParentActive && !frame.ConditionTrue;
                continue;
            }

            if (mnemonic is "ENDC" or "ENDIF")
            {
                if (conditionals.Count == 0)
                {
                    ThrowDiagnostic(sourceName, line.LineNumber, "ENDC/ENDIF without matching IF.");
                }

                var frame = conditionals.Pop();
                isActive = frame.ParentActive;
                continue;
            }

            if (!isActive)
            {
                continue;
            }

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
                pc = EvaluateExpressionOrThrow(line, symbols, pc, sourceName);
                continue;
            }

            if (mnemonic is "FCB" or "DB")
            {
                foreach (var item in SplitCsv(line.OperandText))
                {
                    if (TryParseDbStringLiteral(item, line.LineNumber, sourceName, out var text))
                    {
                        foreach (var ch in text)
                        {
                            EmitByte((byte)ch);
                        }

                        continue;
                    }

                    var value = EvaluateExpressionOrThrow(line with { OperandText = item }, symbols, pc, sourceName);
                    EmitByte((byte)(value & 0xFF));
                }

                continue;
            }

            if (mnemonic == "ASC")
            {
                foreach (var ch in ParseAscText(line.OperandText, line.LineNumber, sourceName))
                {
                    EmitByte((byte)ch);
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

            if (mnemonic is "FDB" or "DW")
            {
                foreach (var item in SplitCsv(line.OperandText))
                {
                    var value = EvaluateExpressionOrThrow(line with { OperandText = item }, symbols, pc, sourceName);
                    EmitByte((byte)((value >> 8) & 0xFF));
                    EmitByte((byte)(value & 0xFF));
                }

                continue;
            }

            if (mnemonic == "RMB")
            {
                pc += EvaluateExpressionOrThrow(line, symbols, pc, sourceName);
                continue;
            }

            if (mnemonic == "DS")
            {
                var (count, fill) = ParseDsOperands(line, symbols, pc, sourceName);
                for (var i = 0; i < count; i++)
                {
                    EmitByte((byte)(fill & 0xFF));
                }

                continue;
            }

            if (mnemonic == "END")
            {
                break;
            }

            if (IsCrAsmNoOpDirective(mnemonic))
            {
                continue;
            }

            if (mnemonic == "BSR" && widenedBsrLines.Contains(line.LineNumber))
            {
                var target = EvaluateExpressionOrThrow(line, symbols, pc, sourceName);
                EmitByte(0xBD); // JSR extended
                EmitByte((byte)((target >> 8) & 0xFF));
                EmitByte((byte)(target & 0xFF));
                continue;
            }

            var normalizedLine = line with { Mnemonic = mnemonic };
            var mode = ResolveAddressingMode(normalizedLine, symbols);
            if (!OpcodeTable.TryGet(mnemonic, mode, out var encoding))
            {
                ThrowDiagnostic(sourceName, line.LineNumber, $"mnemonic '{mnemonic}' does not support {mode} mode.");
            }

            EmitByte(encoding.Opcode);
            EmitOperand(normalizedLine, mode, encoding.OperandBytes, symbols, pc, EmitByte, sourceName);
        }

        FlushSegment();

        if (conditionals.Count > 0)
        {
            ThrowDiagnostic(sourceName, lines[^1].LineNumber, "unterminated IF block (missing ENDC).");
        }

        return segments;
    }

    private static HashSet<int> ResolveFarBsrLines(IReadOnlyList<ParsedLine> lines, string sourceName)
    {
        var widened = new HashSet<int>();
        while (true)
        {
            var symbols = BuildSymbolTable(lines, sourceName, widened);
            var newlyOutOfRange = FindOutOfRangeBsrLines(lines, symbols, sourceName, widened);
            var changed = false;
            foreach (var lineNumber in newlyOutOfRange)
            {
                if (widened.Add(lineNumber))
                {
                    changed = true;
                }
            }

            if (!changed)
            {
                return widened;
            }
        }
    }

    private static HashSet<int> FindOutOfRangeBsrLines(
        IReadOnlyList<ParsedLine> lines,
        IReadOnlyDictionary<string, int> symbols,
        string sourceName,
        ISet<int> widenedBsrLines)
    {
        var outOfRange = new HashSet<int>();
        var pc = 0;
        var conditionals = new Stack<(bool ParentActive, bool ConditionTrue, bool ElseSeen)>();
        var isActive = true;

        foreach (var line in lines)
        {
            var mnemonic = NormalizeMnemonic(line.Mnemonic);
            if (mnemonic == "IF")
            {
                var cond = false;
                if (isActive)
                {
                    cond = EvaluateConditionalExpressionOrThrow(line, symbols, pc, sourceName);
                }

                conditionals.Push((isActive, cond, false));
                isActive = isActive && cond;
                continue;
            }

            if (mnemonic == "ELSE")
            {
                var frame = conditionals.Pop();
                conditionals.Push((frame.ParentActive, frame.ConditionTrue, true));
                isActive = frame.ParentActive && !frame.ConditionTrue;
                continue;
            }

            if (mnemonic is "ENDC" or "ENDIF")
            {
                var frame = conditionals.Pop();
                isActive = frame.ParentActive;
                continue;
            }

            if (!isActive || mnemonic.Length == 0 || mnemonic is "EQU" or "SET")
            {
                continue;
            }

            if (mnemonic == "ORG")
            {
                pc = EvaluateExpressionOrThrow(line, symbols, pc, sourceName);
                continue;
            }

            if (mnemonic is "FCB" or "DB")
            {
                foreach (var item in SplitCsv(line.OperandText))
                {
                    pc += TryParseDbStringLiteral(item, line.LineNumber, sourceName, out var text) ? text.Length : 1;
                }

                continue;
            }

            if (mnemonic == "ASC")
            {
                pc += ParseAscText(line.OperandText, line.LineNumber, sourceName).Length;
                continue;
            }

            if (mnemonic == "FCC")
            {
                pc += ParseFccText(line.OperandText, line.LineNumber, sourceName).Length;
                continue;
            }

            if (mnemonic is "FDB" or "DW")
            {
                pc += SplitCsv(line.OperandText).Count * 2;
                continue;
            }

            if (mnemonic == "RMB")
            {
                pc += EvaluateExpressionOrThrow(line, symbols, pc, sourceName);
                continue;
            }

            if (mnemonic == "DS")
            {
                var (count, _) = ParseDsOperands(line, symbols, pc, sourceName);
                pc += count;
                continue;
            }

            if (mnemonic == "END")
            {
                break;
            }

            if (IsCrAsmNoOpDirective(mnemonic))
            {
                continue;
            }

            if (mnemonic == "BSR" && !widenedBsrLines.Contains(line.LineNumber))
            {
                var target = EvaluateExpressionOrThrow(line, symbols, pc, sourceName);
                var delta = target - (pc + 2);
                if (delta < -128 || delta > 127)
                {
                    outOfRange.Add(line.LineNumber);
                }

                pc += 2;
                continue;
            }

            if (mnemonic == "BSR" && widenedBsrLines.Contains(line.LineNumber))
            {
                pc += 3;
                continue;
            }

            var normalizedLine = line with { Mnemonic = mnemonic };
            var mode = ResolveAddressingMode(normalizedLine, symbols);
            if (!OpcodeTable.TryGet(mnemonic, mode, out var encoding))
            {
                ThrowDiagnostic(sourceName, line.LineNumber, $"mnemonic '{mnemonic}' does not support {mode} mode.");
            }

            pc += 1 + encoding.OperandBytes;
        }

        return outOfRange;
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
            var offset = ParseIndexedOffset(line, symbols, pcAfterOpcode - 1, sourceName);
            emitByte((byte)(offset & 0xFF));
            return;
        }

        if (mode == AddressingMode.Relative)
        {
            var target = EvaluateExpressionOrThrow(line, symbols, pcAfterOpcode - 1, sourceName);
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

        var value = EvaluateExpressionOrThrow(line, symbols, pcAfterOpcode - 1, sourceName);
        if (operandBytes == 1)
        {
            emitByte((byte)(value & 0xFF));
            return;
        }

        emitByte((byte)((value >> 8) & 0xFF));
        emitByte((byte)(value & 0xFF));
    }

    private static int ParseIndexedOffset(
        ParsedLine line,
        IReadOnlyDictionary<string, int> symbols,
        int currentAddress,
        string sourceName)
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

        var value = EvaluateExpressionOrThrow(line with { OperandText = parts[0] }, symbols, currentAddress, sourceName);
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

    private static int EvaluateExpressionOrThrow(
        ParsedLine line,
        IReadOnlyDictionary<string, int> symbols,
        int currentAddress,
        string sourceName)
    {
        if (!ExpressionEvaluator.TryEvaluate(line.OperandText, symbols, out var value, out var unresolved, currentAddress))
        {
            ThrowDiagnostic(
                sourceName,
                line.LineNumber,
                $"cannot evaluate expression '{line.OperandText}' (unresolved: {unresolved ?? "unknown"}).");
        }

        return value;
    }

    private static bool EvaluateConditionalExpressionOrThrow(
        ParsedLine line,
        IReadOnlyDictionary<string, int> symbols,
        int currentAddress,
        string sourceName)
    {
        var operand = line.OperandText.Trim();
        if (operand.Length == 0)
        {
            ThrowDiagnostic(sourceName, line.LineNumber, "IF requires a condition expression.");
        }

        if (TrySplitConditionalExpression(operand, out var left, out var op, out var right))
        {
            var leftValue = EvaluateExpressionOrThrow(line with { OperandText = left }, symbols, currentAddress, sourceName);
            var rightValue = EvaluateExpressionOrThrow(line with { OperandText = right }, symbols, currentAddress, sourceName);
            return op switch
            {
                "=" or "==" => leftValue == rightValue,
                "<>" => leftValue != rightValue,
                "<" => leftValue < rightValue,
                ">" => leftValue > rightValue,
                "<=" => leftValue <= rightValue,
                ">=" => leftValue >= rightValue,
                _ => throw new InvalidOperationException($"{sourceName}:{line.LineNumber}: unsupported IF comparator '{op}'.")
            };
        }

        var value = EvaluateExpressionOrThrow(line, symbols, currentAddress, sourceName);
        return value != 0;
    }

    private static bool TrySplitConditionalExpression(string operand, out string left, out string op, out string right)
    {
        var operators = new[] { "==", "<>", "<=", ">=", "<", ">", "=" };
        foreach (var candidate in operators)
        {
            var index = operand.IndexOf(candidate, StringComparison.Ordinal);
            if (index <= 0 || index >= operand.Length - candidate.Length)
            {
                continue;
            }

            left = operand[..index].Trim();
            right = operand[(index + candidate.Length)..].Trim();
            op = candidate;
            if (left.Length == 0 || right.Length == 0)
            {
                continue;
            }

            return true;
        }

        left = string.Empty;
        op = string.Empty;
        right = string.Empty;
        return false;
    }

    private static bool LooksLikeMasmX86Dialect(ParsedLine line)
    {
        if (LikelyMasmX86Tokens.Contains(line.Mnemonic))
        {
            return true;
        }

        if (line.OperandText.Length == 0)
        {
            return false;
        }

        var parts = line.OperandText.Split([' ', '\t', ','], StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (LikelyMasmX86Tokens.Contains(part.ToUpperInvariant()))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCrAsmNoOpDirective(string mnemonic)
    {
        return mnemonic is
            "CPU" or
            "OUTPUT" or
            "CODE" or
            "DUMMY" or
            "BSS" or
            "PAGE" or
            "LIST" or
            "CLIST" or
            "MLIST" or
            "ILIST" or
            "NAM" or
            "TITLE";
    }

    private static string NormalizeMnemonic(string mnemonic)
    {
        return mnemonic switch
        {
            "LDA" => "LDAA",
            "LDB" => "LDAB",
            "STA" => "STAA",
            "STB" => "STAB",
            "ORA" => "ORAA",
            _ => mnemonic
        };
    }

    private static int ResolveDataBytesPerRecord(IReadOnlyList<string> sourceLines, int configuredDataBytesPerRecord)
    {
        if (configuredDataBytesPerRecord != 32)
        {
            return configuredDataBytesPerRecord;
        }

        var sawCpu6800Family = false;
        for (var i = 0; i < sourceLines.Count; i++)
        {
            var parsed = LineParser.Parse(sourceLines[i], i + 1);
            if (parsed is null)
            {
                continue;
            }

            if (parsed.Mnemonic.Equals("OUTPUT", StringComparison.OrdinalIgnoreCase) &&
                parsed.OperandText.Trim().Equals("SCODE", StringComparison.OrdinalIgnoreCase))
            {
                return 16;
            }

            if (parsed.Mnemonic.Equals("CPU", StringComparison.OrdinalIgnoreCase))
            {
                var cpuName = parsed.OperandText.Trim();
                if (cpuName.Equals("6800", StringComparison.OrdinalIgnoreCase) ||
                    cpuName.Equals("6801", StringComparison.OrdinalIgnoreCase) ||
                    cpuName.Equals("6803", StringComparison.OrdinalIgnoreCase) ||
                    cpuName.Equals("6808", StringComparison.OrdinalIgnoreCase))
                {
                    sawCpu6800Family = true;
                }
            }
        }

        return sawCpu6800Family ? 16 : configuredDataBytesPerRecord;
    }

    private static List<string> SplitCsv(string operandText)
    {
        var items = new List<string>();
        var current = new System.Text.StringBuilder();
        var inSingleQuote = false;
        var inDoubleQuote = false;

        for (var i = 0; i < operandText.Length; i++)
        {
            var ch = operandText[i];
            if (ch == '\'' && !inDoubleQuote)
            {
                inSingleQuote = !inSingleQuote;
                current.Append(ch);
                continue;
            }

            if (ch == '"' && !inSingleQuote)
            {
                inDoubleQuote = !inDoubleQuote;
                current.Append(ch);
                continue;
            }

            if (ch == ',' && !inSingleQuote && !inDoubleQuote)
            {
                var part = current.ToString().Trim();
                if (part.Length > 0)
                {
                    items.Add(part);
                }

                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        var last = current.ToString().Trim();
        if (last.Length > 0)
        {
            items.Add(last);
        }

        return items;
    }

    private static bool TryParseDbStringLiteral(string item, int lineNumber, string sourceName, out string text)
    {
        var trimmed = item.Trim();
        if (trimmed.Length >= 2 &&
            ((trimmed[0] == '"' && trimmed[^1] == '"') || (trimmed[0] == '\'' && trimmed[^1] == '\'')))
        {
            text = ParseAscText(trimmed, lineNumber, sourceName);
            return true;
        }

        text = string.Empty;
        return false;
    }

    private static (int Count, int Fill) ParseDsOperands(
        ParsedLine line,
        IReadOnlyDictionary<string, int> symbols,
        int currentAddress,
        string sourceName)
    {
        var parts = SplitCsv(line.OperandText);
        if (parts.Count is < 1 or > 2)
        {
            ThrowDiagnostic(sourceName, line.LineNumber, "DS expects 'count' or 'count,fill'.");
        }

        var count = EvaluateExpressionOrThrow(line with { OperandText = parts[0] }, symbols, currentAddress, sourceName);
        if (count < 0)
        {
            ThrowDiagnostic(sourceName, line.LineNumber, "DS count must be non-negative.");
        }

        var fill = 0;
        if (parts.Count == 2)
        {
            fill = EvaluateExpressionOrThrow(line with { OperandText = parts[1] }, symbols, currentAddress, sourceName);
        }

        return (count, fill);
    }

    private static string ParseAscText(string operandText, int lineNumber, string sourceName)
    {
        var trimmed = operandText.Trim();
        if (trimmed.Length < 2)
        {
            ThrowDiagnostic(sourceName, lineNumber, "ASC requires a quoted string literal.");
        }

        var delimiter = trimmed[0];
        if (delimiter is not ('"' or '\'') || trimmed[^1] != delimiter)
        {
            ThrowDiagnostic(sourceName, lineNumber, "ASC requires matching string quotes.");
        }

        var content = trimmed[1..^1];
        var sb = new System.Text.StringBuilder(content.Length);
        for (var i = 0; i < content.Length; i++)
        {
            if (content[i] != '\\')
            {
                sb.Append(content[i]);
                continue;
            }

            i++;
            if (i >= content.Length)
            {
                ThrowDiagnostic(sourceName, lineNumber, "ASC has an incomplete escape sequence.");
            }

            sb.Append(content[i] switch
            {
                'r' => '\r',
                'n' => '\n',
                't' => '\t',
                '0' => '\0',
                '\'' => '\'',
                '"' => '"',
                '\\' => '\\',
                _ => content[i]
            });
        }

        return sb.ToString();
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



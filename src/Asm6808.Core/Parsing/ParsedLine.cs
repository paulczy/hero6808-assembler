namespace Asm6808.Core.Parsing;

public sealed record ParsedLine(
    int LineNumber,
    string RawText,
    string? Label,
    string Mnemonic,
    string OperandText);


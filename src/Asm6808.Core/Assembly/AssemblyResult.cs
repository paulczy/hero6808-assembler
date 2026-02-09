namespace Asm6808.Core.Assembly;

public sealed record AssemblyResult(
    IReadOnlyList<AddressedBytes> Segments,
    IReadOnlyDictionary<string, int> Symbols);


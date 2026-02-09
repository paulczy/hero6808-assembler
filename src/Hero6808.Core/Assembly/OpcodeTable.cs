namespace Hero6808.Core.Assembly;

internal readonly record struct OpcodeEncoding(byte Opcode, int OperandBytes);

internal static class OpcodeTable
{
    private static readonly Dictionary<string, Dictionary<AddressingMode, OpcodeEncoding>> Table =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ABA"] = new() { [AddressingMode.Inherent] = new(0x1B, 0) },
            ["ADDA"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0x8B, 1),
                    [AddressingMode.Direct] = new(0x9B, 1),
                    [AddressingMode.Indexed] = new(0xAB, 1),
                    [AddressingMode.Extended] = new(0xBB, 2)
                },
            ["ANDA"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0x84, 1),
                    [AddressingMode.Direct] = new(0x94, 1),
                    [AddressingMode.Indexed] = new(0xA4, 1),
                    [AddressingMode.Extended] = new(0xB4, 2)
                },
            ["ASLA"] = new() { [AddressingMode.Inherent] = new(0x48, 0) },
            ["BCC"] = new() { [AddressingMode.Relative] = new(0x24, 1) },
            ["BEQ"] = new() { [AddressingMode.Relative] = new(0x27, 1) },
            ["BHI"] = new() { [AddressingMode.Relative] = new(0x22, 1) },
            ["BLS"] = new() { [AddressingMode.Relative] = new(0x23, 1) },
            ["BNE"] = new() { [AddressingMode.Relative] = new(0x26, 1) },
            ["BRA"] = new() { [AddressingMode.Relative] = new(0x20, 1) },
            ["BSR"] = new() { [AddressingMode.Relative] = new(0x8D, 1) },
            ["CLI"] = new() { [AddressingMode.Inherent] = new(0x0E, 0) },
            ["CLR"] =
                new()
                {
                    [AddressingMode.Indexed] = new(0x6F, 1),
                    [AddressingMode.Extended] = new(0x7F, 2)
                },
            ["CLRA"] = new() { [AddressingMode.Inherent] = new(0x4F, 0) },
            ["CLRB"] = new() { [AddressingMode.Inherent] = new(0x5F, 0) },
            ["CMPA"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0x81, 1),
                    [AddressingMode.Direct] = new(0x91, 1),
                    [AddressingMode.Indexed] = new(0xA1, 1),
                    [AddressingMode.Extended] = new(0xB1, 2)
                },
            ["DEX"] = new() { [AddressingMode.Inherent] = new(0x09, 0) },
            ["INCA"] = new() { [AddressingMode.Inherent] = new(0x4C, 0) },
            ["INX"] = new() { [AddressingMode.Inherent] = new(0x08, 0) },
            ["JMP"] =
                new()
                {
                    [AddressingMode.Indexed] = new(0x6E, 1),
                    [AddressingMode.Extended] = new(0x7E, 2)
                },
            ["JSR"] =
                new()
                {
                    [AddressingMode.Indexed] = new(0xAD, 1),
                    [AddressingMode.Extended] = new(0xBD, 2)
                },
            ["LDAA"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0x86, 1),
                    [AddressingMode.Direct] = new(0x96, 1),
                    [AddressingMode.Indexed] = new(0xA6, 1),
                    [AddressingMode.Extended] = new(0xB6, 2)
                },
            ["LDAB"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0xC6, 1),
                    [AddressingMode.Direct] = new(0xD6, 1),
                    [AddressingMode.Indexed] = new(0xE6, 1),
                    [AddressingMode.Extended] = new(0xF6, 2)
                },
            ["LDX"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0xCE, 2),
                    [AddressingMode.Direct] = new(0xDE, 1),
                    [AddressingMode.Indexed] = new(0xEE, 1),
                    [AddressingMode.Extended] = new(0xFE, 2)
                },
            ["LSRA"] = new() { [AddressingMode.Inherent] = new(0x44, 0) },
            ["ORAA"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0x8A, 1),
                    [AddressingMode.Direct] = new(0x9A, 1),
                    [AddressingMode.Indexed] = new(0xAA, 1),
                    [AddressingMode.Extended] = new(0xBA, 2)
                },
            ["PSHA"] = new() { [AddressingMode.Inherent] = new(0x36, 0) },
            ["PULA"] = new() { [AddressingMode.Inherent] = new(0x32, 0) },
            ["RTS"] = new() { [AddressingMode.Inherent] = new(0x39, 0) },
            ["SEI"] = new() { [AddressingMode.Inherent] = new(0x0F, 0) },
            ["STAA"] =
                new()
                {
                    [AddressingMode.Direct] = new(0x97, 1),
                    [AddressingMode.Indexed] = new(0xA7, 1),
                    [AddressingMode.Extended] = new(0xB7, 2)
                },
            ["STAB"] =
                new()
                {
                    [AddressingMode.Direct] = new(0xD7, 1),
                    [AddressingMode.Indexed] = new(0xE7, 1),
                    [AddressingMode.Extended] = new(0xF7, 2)
                },
            ["STX"] =
                new()
                {
                    [AddressingMode.Direct] = new(0xDF, 1),
                    [AddressingMode.Indexed] = new(0xEF, 1),
                    [AddressingMode.Extended] = new(0xFF, 2)
                },
            ["SUBA"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0x80, 1),
                    [AddressingMode.Direct] = new(0x90, 1),
                    [AddressingMode.Indexed] = new(0xA0, 1),
                    [AddressingMode.Extended] = new(0xB0, 2)
                },
            ["SWI"] = new() { [AddressingMode.Inherent] = new(0x3F, 0) },
            ["TAB"] = new() { [AddressingMode.Inherent] = new(0x16, 0) },
            ["TBA"] = new() { [AddressingMode.Inherent] = new(0x17, 0) },
            ["TSTA"] = new() { [AddressingMode.Inherent] = new(0x4D, 0) },
            ["TSTB"] = new() { [AddressingMode.Inherent] = new(0x5D, 0) }
        };

    public static bool IsKnownMnemonic(string mnemonic) => Table.ContainsKey(mnemonic);

    public static bool TryGet(string mnemonic, AddressingMode mode, out OpcodeEncoding encoding)
    {
        encoding = default;
        return Table.TryGetValue(mnemonic, out var variants) && variants.TryGetValue(mode, out encoding);
    }

    public static bool SupportsMode(string mnemonic, AddressingMode mode)
    {
        return Table.TryGetValue(mnemonic, out var variants) && variants.ContainsKey(mode);
    }
}



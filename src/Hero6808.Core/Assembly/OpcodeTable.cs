namespace Hero6808.Core.Assembly;

internal readonly record struct OpcodeEncoding(byte Opcode, int OperandBytes);

internal static class OpcodeTable
{
    private static readonly Dictionary<string, Dictionary<AddressingMode, OpcodeEncoding>> Table =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ABA"] = new() { [AddressingMode.Inherent] = new(0x1B, 0) },
            ["ADCA"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0x89, 1),
                    [AddressingMode.Direct] = new(0x99, 1),
                    [AddressingMode.Indexed] = new(0xA9, 1),
                    [AddressingMode.Extended] = new(0xB9, 2)
                },
            ["ADCB"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0xC9, 1),
                    [AddressingMode.Direct] = new(0xD9, 1),
                    [AddressingMode.Indexed] = new(0xE9, 1),
                    [AddressingMode.Extended] = new(0xF9, 2)
                },
            ["ADDA"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0x8B, 1),
                    [AddressingMode.Direct] = new(0x9B, 1),
                    [AddressingMode.Indexed] = new(0xAB, 1),
                    [AddressingMode.Extended] = new(0xBB, 2)
                },
            ["ADDB"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0xCB, 1),
                    [AddressingMode.Direct] = new(0xDB, 1),
                    [AddressingMode.Indexed] = new(0xEB, 1),
                    [AddressingMode.Extended] = new(0xFB, 2)
                },
            ["ANDA"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0x84, 1),
                    [AddressingMode.Direct] = new(0x94, 1),
                    [AddressingMode.Indexed] = new(0xA4, 1),
                    [AddressingMode.Extended] = new(0xB4, 2)
                },
            ["ANDB"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0xC4, 1),
                    [AddressingMode.Direct] = new(0xD4, 1),
                    [AddressingMode.Indexed] = new(0xE4, 1),
                    [AddressingMode.Extended] = new(0xF4, 2)
                },
            ["ASL"] =
                new()
                {
                    [AddressingMode.Indexed] = new(0x68, 1),
                    [AddressingMode.Extended] = new(0x78, 2)
                },
            ["ASLA"] = new() { [AddressingMode.Inherent] = new(0x48, 0) },
            ["ASLB"] = new() { [AddressingMode.Inherent] = new(0x58, 0) },
            ["ASR"] =
                new()
                {
                    [AddressingMode.Indexed] = new(0x67, 1),
                    [AddressingMode.Extended] = new(0x77, 2)
                },
            ["ASRA"] = new() { [AddressingMode.Inherent] = new(0x47, 0) },
            ["ASRB"] = new() { [AddressingMode.Inherent] = new(0x57, 0) },
            ["BCC"] = new() { [AddressingMode.Relative] = new(0x24, 1) },
            ["BCS"] = new() { [AddressingMode.Relative] = new(0x25, 1) },
            ["BEQ"] = new() { [AddressingMode.Relative] = new(0x27, 1) },
            ["BGE"] = new() { [AddressingMode.Relative] = new(0x2C, 1) },
            ["BGT"] = new() { [AddressingMode.Relative] = new(0x2E, 1) },
            ["BITA"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0x85, 1),
                    [AddressingMode.Direct] = new(0x95, 1),
                    [AddressingMode.Indexed] = new(0xA5, 1),
                    [AddressingMode.Extended] = new(0xB5, 2)
                },
            ["BITB"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0xC5, 1),
                    [AddressingMode.Direct] = new(0xD5, 1),
                    [AddressingMode.Indexed] = new(0xE5, 1),
                    [AddressingMode.Extended] = new(0xF5, 2)
                },
            ["BHI"] = new() { [AddressingMode.Relative] = new(0x22, 1) },
            ["BLE"] = new() { [AddressingMode.Relative] = new(0x2F, 1) },
            ["BLS"] = new() { [AddressingMode.Relative] = new(0x23, 1) },
            ["BLT"] = new() { [AddressingMode.Relative] = new(0x2D, 1) },
            ["BMI"] = new() { [AddressingMode.Relative] = new(0x2B, 1) },
            ["BNE"] = new() { [AddressingMode.Relative] = new(0x26, 1) },
            ["BPL"] = new() { [AddressingMode.Relative] = new(0x2A, 1) },
            ["BRA"] = new() { [AddressingMode.Relative] = new(0x20, 1) },
            ["BSR"] = new() { [AddressingMode.Relative] = new(0x8D, 1) },
            ["BVC"] = new() { [AddressingMode.Relative] = new(0x28, 1) },
            ["BVS"] = new() { [AddressingMode.Relative] = new(0x29, 1) },
            ["CBA"] = new() { [AddressingMode.Inherent] = new(0x11, 0) },
            ["CLC"] = new() { [AddressingMode.Inherent] = new(0x0C, 0) },
            ["CLI"] = new() { [AddressingMode.Inherent] = new(0x0E, 0) },
            ["CLR"] =
                new()
                {
                    [AddressingMode.Indexed] = new(0x6F, 1),
                    [AddressingMode.Extended] = new(0x7F, 2)
                },
            ["CLRA"] = new() { [AddressingMode.Inherent] = new(0x4F, 0) },
            ["CLRB"] = new() { [AddressingMode.Inherent] = new(0x5F, 0) },
            ["CLV"] = new() { [AddressingMode.Inherent] = new(0x0A, 0) },
            ["CMPA"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0x81, 1),
                    [AddressingMode.Direct] = new(0x91, 1),
                    [AddressingMode.Indexed] = new(0xA1, 1),
                    [AddressingMode.Extended] = new(0xB1, 2)
                },
            ["CMPB"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0xC1, 1),
                    [AddressingMode.Direct] = new(0xD1, 1),
                    [AddressingMode.Indexed] = new(0xE1, 1),
                    [AddressingMode.Extended] = new(0xF1, 2)
                },
            ["COM"] =
                new()
                {
                    [AddressingMode.Indexed] = new(0x63, 1),
                    [AddressingMode.Extended] = new(0x73, 2)
                },
            ["COMA"] = new() { [AddressingMode.Inherent] = new(0x43, 0) },
            ["COMB"] = new() { [AddressingMode.Inherent] = new(0x53, 0) },
            ["CPX"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0x8C, 2),
                    [AddressingMode.Direct] = new(0x9C, 1),
                    [AddressingMode.Indexed] = new(0xAC, 1),
                    [AddressingMode.Extended] = new(0xBC, 2)
                },
            ["DAA"] = new() { [AddressingMode.Inherent] = new(0x19, 0) },
            ["DECA"] = new() { [AddressingMode.Inherent] = new(0x4A, 0) },
            ["DEC"] =
                new()
                {
                    [AddressingMode.Indexed] = new(0x6A, 1),
                    [AddressingMode.Extended] = new(0x7A, 2)
                },
            ["DEX"] = new() { [AddressingMode.Inherent] = new(0x09, 0) },
            ["DES"] = new() { [AddressingMode.Inherent] = new(0x34, 0) },
            ["EORA"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0x88, 1),
                    [AddressingMode.Direct] = new(0x98, 1),
                    [AddressingMode.Indexed] = new(0xA8, 1),
                    [AddressingMode.Extended] = new(0xB8, 2)
                },
            ["EORB"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0xC8, 1),
                    [AddressingMode.Direct] = new(0xD8, 1),
                    [AddressingMode.Indexed] = new(0xE8, 1),
                    [AddressingMode.Extended] = new(0xF8, 2)
                },
            ["INCA"] = new() { [AddressingMode.Inherent] = new(0x4C, 0) },
            ["INC"] =
                new()
                {
                    [AddressingMode.Indexed] = new(0x6C, 1),
                    [AddressingMode.Extended] = new(0x7C, 2)
                },
            ["INS"] = new() { [AddressingMode.Inherent] = new(0x31, 0) },
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
            ["LDS"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0x8E, 2),
                    [AddressingMode.Direct] = new(0x9E, 1),
                    [AddressingMode.Indexed] = new(0xAE, 1),
                    [AddressingMode.Extended] = new(0xBE, 2)
                },
            ["LDX"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0xCE, 2),
                    [AddressingMode.Direct] = new(0xDE, 1),
                    [AddressingMode.Indexed] = new(0xEE, 1),
                    [AddressingMode.Extended] = new(0xFE, 2)
                },
            ["LSR"] =
                new()
                {
                    [AddressingMode.Indexed] = new(0x64, 1),
                    [AddressingMode.Extended] = new(0x74, 2)
                },
            ["LSRA"] = new() { [AddressingMode.Inherent] = new(0x44, 0) },
            ["LSRB"] = new() { [AddressingMode.Inherent] = new(0x54, 0) },
            ["NEG"] =
                new()
                {
                    [AddressingMode.Indexed] = new(0x60, 1),
                    [AddressingMode.Extended] = new(0x70, 2)
                },
            ["NEGA"] = new() { [AddressingMode.Inherent] = new(0x40, 0) },
            ["NEGB"] = new() { [AddressingMode.Inherent] = new(0x50, 0) },
            ["NOP"] = new() { [AddressingMode.Inherent] = new(0x01, 0) },
            ["ORAA"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0x8A, 1),
                    [AddressingMode.Direct] = new(0x9A, 1),
                    [AddressingMode.Indexed] = new(0xAA, 1),
                    [AddressingMode.Extended] = new(0xBA, 2)
                },
            ["ORAB"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0xCA, 1),
                    [AddressingMode.Direct] = new(0xDA, 1),
                    [AddressingMode.Indexed] = new(0xEA, 1),
                    [AddressingMode.Extended] = new(0xFA, 2)
                },
            ["PSHA"] = new() { [AddressingMode.Inherent] = new(0x36, 0) },
            ["PULA"] = new() { [AddressingMode.Inherent] = new(0x32, 0) },
            ["PSHX"] = new() { [AddressingMode.Inherent] = new(0x3C, 0) },
            ["PULX"] = new() { [AddressingMode.Inherent] = new(0x38, 0) },
            ["ROL"] =
                new()
                {
                    [AddressingMode.Indexed] = new(0x69, 1),
                    [AddressingMode.Extended] = new(0x79, 2)
                },
            ["ROLA"] = new() { [AddressingMode.Inherent] = new(0x49, 0) },
            ["ROLB"] = new() { [AddressingMode.Inherent] = new(0x59, 0) },
            ["ROR"] =
                new()
                {
                    [AddressingMode.Indexed] = new(0x66, 1),
                    [AddressingMode.Extended] = new(0x76, 2)
                },
            ["RORA"] = new() { [AddressingMode.Inherent] = new(0x46, 0) },
            ["RORB"] = new() { [AddressingMode.Inherent] = new(0x56, 0) },
            ["RTS"] = new() { [AddressingMode.Inherent] = new(0x39, 0) },
            ["SBA"] = new() { [AddressingMode.Inherent] = new(0x10, 0) },
            ["SBCA"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0x82, 1),
                    [AddressingMode.Direct] = new(0x92, 1),
                    [AddressingMode.Indexed] = new(0xA2, 1),
                    [AddressingMode.Extended] = new(0xB2, 2)
                },
            ["SBCB"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0xC2, 1),
                    [AddressingMode.Direct] = new(0xD2, 1),
                    [AddressingMode.Indexed] = new(0xE2, 1),
                    [AddressingMode.Extended] = new(0xF2, 2)
                },
            ["SEC"] = new() { [AddressingMode.Inherent] = new(0x0D, 0) },
            ["SEI"] = new() { [AddressingMode.Inherent] = new(0x0F, 0) },
            ["SEV"] = new() { [AddressingMode.Inherent] = new(0x0B, 0) },
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
            ["STS"] =
                new()
                {
                    [AddressingMode.Direct] = new(0x9F, 1),
                    [AddressingMode.Indexed] = new(0xAF, 1),
                    [AddressingMode.Extended] = new(0xBF, 2)
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
            ["SUBB"] =
                new()
                {
                    [AddressingMode.Immediate] = new(0xC0, 1),
                    [AddressingMode.Direct] = new(0xD0, 1),
                    [AddressingMode.Indexed] = new(0xE0, 1),
                    [AddressingMode.Extended] = new(0xF0, 2)
                },
            ["DECB"] = new() { [AddressingMode.Inherent] = new(0x5A, 0) },
            ["INCB"] = new() { [AddressingMode.Inherent] = new(0x5C, 0) },
            ["PSHB"] = new() { [AddressingMode.Inherent] = new(0x37, 0) },
            ["PULB"] = new() { [AddressingMode.Inherent] = new(0x33, 0) },
            ["RTI"] = new() { [AddressingMode.Inherent] = new(0x3B, 0) },
            ["SWI"] = new() { [AddressingMode.Inherent] = new(0x3F, 0) },
            ["TAB"] = new() { [AddressingMode.Inherent] = new(0x16, 0) },
            ["TAP"] = new() { [AddressingMode.Inherent] = new(0x06, 0) },
            ["TBA"] = new() { [AddressingMode.Inherent] = new(0x17, 0) },
            ["TPA"] = new() { [AddressingMode.Inherent] = new(0x07, 0) },
            ["TST"] =
                new()
                {
                    [AddressingMode.Indexed] = new(0x6D, 1),
                    [AddressingMode.Extended] = new(0x7D, 2)
                },
            ["TSTA"] = new() { [AddressingMode.Inherent] = new(0x4D, 0) },
            ["TSTB"] = new() { [AddressingMode.Inherent] = new(0x5D, 0) },
            ["TSX"] = new() { [AddressingMode.Inherent] = new(0x30, 0) },
            ["TXS"] = new() { [AddressingMode.Inherent] = new(0x35, 0) },
            ["WAI"] = new() { [AddressingMode.Inherent] = new(0x3E, 0) }
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



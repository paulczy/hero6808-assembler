using Hero6808.Core.Assembly;

namespace Hero6808.Tests;

public class Assembler6800Tests
{
    [Test]
    public async Task Assemble_EncodesSimpleProgramWithRelativeBranch()
    {
        var source = new[]
        {
            "ORG $0200",
            "start: LDAA #1",
            "       STAA $10",
            "       BRA start",
            "END"
        };

        var assembler = new Assembler6800();
        var result = assembler.Assemble(source);

        await Assert.That(result.Segments.Count).IsEqualTo(1);
        var segment = result.Segments[0];
        await Assert.That(segment.StartAddress).IsEqualTo((ushort)0x0200);
        await Assert.That(segment.Data.SequenceEqual(new byte[] { 0x86, 0x01, 0x97, 0x10, 0x20, 0xFA })).IsTrue();
    }

    [Test]
    public async Task Assemble_GotAHeroCorpus_MatchesExpectedS19()
    {
        var root = FindRepoRoot();
        var asmPath = Path.Combine(root, "tests", "corpus", "local-gotahero", "GotAHero.asm");
        var expectedPath = Path.Combine(root, "tests", "corpus", "local-gotahero", "GOTAHERO.S19");

        var assembler = new Assembler6800();
        var actual = assembler.AssembleToS19Records(File.ReadLines(asmPath), sourceName: asmPath);
        var expected = File.ReadAllLines(expectedPath);

        await Assert.That(actual.SequenceEqual(expected)).IsTrue();
    }

    [Test]
    public async Task Assemble_HeroCommonMnemonicsFixture_EncodesExpectedBytes()
    {
        var root = FindRepoRoot();
        var asmPath = Path.Combine(root, "tests", "corpus", "hero-common-mnemonics", "common_stack_and_bit.asm");

        var assembler = new Assembler6800();
        var result = assembler.Assemble(File.ReadLines(asmPath), sourceName: asmPath);

        await Assert.That(result.Segments.Count).IsEqualTo(1);
        var segment = result.Segments[0];
        await Assert.That(segment.StartAddress).IsEqualTo((ushort)0x0100);
        await Assert.That(segment.Data.SequenceEqual(
                new byte[]
                {
                    0x8E, 0x0F, 0xDF, // LDS #$0FDF
                    0x9F, 0x20,       // STS $20
                    0x85, 0x02,       // BITA #$02
                    0xD5, 0x30,       // BITB $30
                    0x5C,             // INCB
                    0x5A,             // DECB
                    0x37,             // PSHB
                    0x33,             // PULB
                    0x30,             // TSX
                    0x35,             // TXS
                    0x3B              // RTI
                }))
            .IsTrue();
    }

    [Test]
    public async Task Assemble_HeroCommonIndexedFixture_EncodesExpectedBytes()
    {
        var root = FindRepoRoot();
        var asmPath = Path.Combine(root, "tests", "corpus", "hero-common-mnemonics", "common_stack_and_bit_indexed.asm");

        var assembler = new Assembler6800();
        var result = assembler.Assemble(File.ReadLines(asmPath), sourceName: asmPath);

        await Assert.That(result.Segments.Count).IsEqualTo(1);
        var segment = result.Segments[0];
        await Assert.That(segment.StartAddress).IsEqualTo((ushort)0x0120);
        await Assert.That(segment.Data.SequenceEqual(
                new byte[]
                {
                    0xBE, 0x12, 0x34, // LDS $1234
                    0xBF, 0x12, 0x34, // STS $1234
                    0xA5, 0x10,       // BITA $10,X
                    0xE5, 0x20        // BITB $20,X
                }))
            .IsTrue();
    }

    [Test]
    public async Task Assemble_ReportsDiagnosticWithFileLineFormat_OnUnknownMnemonic()
    {
        var source = new[]
        {
            "ORG $0200",
            "BOGUS #1",
            "END"
        };

        var assembler = new Assembler6800();
        var threw = false;
        var message = string.Empty;

        try
        {
            assembler.Assemble(source, sourceName: "bad.asm");
        }
        catch (InvalidOperationException ex)
        {
            threw = true;
            message = ex.Message;
        }

        await Assert.That(threw).IsTrue();
        await Assert.That(message.StartsWith("bad.asm:2:")).IsTrue();
        await Assert.That(message.Contains("unknown mnemonic")).IsTrue();
    }

    [Test]
    public async Task Assemble_ReportsDiagnosticWithFileLineFormat_OnUnresolvedSymbol()
    {
        var source = new[]
        {
            "ORG $0200",
            "LDAA MissingLabel",
            "END"
        };

        var assembler = new Assembler6800();
        var threw = false;
        var message = string.Empty;

        try
        {
            assembler.Assemble(source, sourceName: "bad.asm");
        }
        catch (InvalidOperationException ex)
        {
            threw = true;
            message = ex.Message;
        }

        await Assert.That(threw).IsTrue();
        await Assert.That(message.StartsWith("bad.asm:2:")).IsTrue();
        await Assert.That(message.Contains("cannot evaluate expression")).IsTrue();
    }

    [Test]
    public async Task Assemble_UsesDirectModeForLowAddressEquSymbols()
    {
        var source = new[]
        {
            "sonar equ $11",
            "org $0400",
            "ldaa sonar",
            "end"
        };

        var assembler = new Assembler6800();
        var result = assembler.Assemble(source, sourceName: "direct.asm");
        var segment = result.Segments[0];

        await Assert.That(segment.Data.SequenceEqual(new byte[] { 0x96, 0x11 })).IsTrue();
    }

    [Test]
    public async Task Assemble_HeroJrAnalysisFixture_PreservesStackAndDelayInstructions()
    {
        var root = FindRepoRoot();
        var asmPath = Path.Combine(root, "tests", "corpus", "hero-jr-analysis", "hero_jr_from_md.asm");

        var assembler = new Assembler6800();
        var result = assembler.Assemble(File.ReadLines(asmPath), sourceName: asmPath);

        await Assert.That(result.Segments.Count).IsEqualTo(2);

        var code = result.Segments.Single(s => s.StartAddress == 0xE000).Data;

        // SAY_HELLO_WORLD prologue/epilogue must preserve X.
        await Assert.That(ContainsSubsequence(code, 0x36, 0x3C, 0xCE)).IsTrue();
        await Assert.That(ContainsSubsequence(code, 0x38, 0x32, 0x39)).IsTrue();

        // DRIVE_CIRCLE and DELAY_1SEC must include DECA in their outer loops.
        await Assert.That(ContainsSubsequence(code, 0x5A, 0x26, 0xFA, 0x4A, 0x26)).IsTrue();
        await Assert.That(ContainsSubsequence(code, 0x5A, 0x26, 0xF9, 0x4A, 0x26)).IsTrue();

        // DELAY_1SEC and DELAY_SHORT must include NOP timing pads.
        await Assert.That(ContainsSubsequence(code, 0x01, 0x01, 0x01, 0x01, 0x5A, 0x26)).IsTrue();
        await Assert.That(ContainsSubsequence(code, 0x01, 0x5A, 0x26, 0xFC)).IsTrue();
    }

    private static bool ContainsSubsequence(byte[] data, params byte[] pattern)
    {
        if (pattern.Length == 0 || pattern.Length > data.Length)
        {
            return false;
        }

        for (var i = 0; i <= data.Length - pattern.Length; i++)
        {
            var match = true;
            for (var j = 0; j < pattern.Length; j++)
            {
                if (data[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return true;
            }
        }

        return false;
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var slnx = Path.Combine(current.FullName, "Hero6808.slnx");
            if (File.Exists(slnx))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test base directory.");
    }
}




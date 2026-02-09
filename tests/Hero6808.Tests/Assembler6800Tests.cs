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




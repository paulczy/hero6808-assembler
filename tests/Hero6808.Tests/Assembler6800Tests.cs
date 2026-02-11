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
    public async Task Assemble_ReportsMasmDialectDiagnostic_ForX86StyleSource()
    {
        var source = new[]
        {
            "DATA SEGMENT PARA 'DATA'",
            "NIBH DB ?",
            "DATA ENDS"
        };

        var assembler = new Assembler6800();
        var threw = false;
        var message = string.Empty;

        try
        {
            assembler.Assemble(source, sourceName: "sendbyte.asm");
        }
        catch (InvalidOperationException ex)
        {
            threw = true;
            message = ex.Message;
        }

        await Assert.That(threw).IsTrue();
        await Assert.That(message.StartsWith("sendbyte.asm:1:")).IsTrue();
        await Assert.That(message.Contains("MASM/x86-style syntax")).IsTrue();
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
    public async Task Assemble_FccAcceptsSingleQuotedString()
    {
        var source = new[]
        {
            "ORG $0200",
            "FCC 'HI'",
            "END"
        };

        var assembler = new Assembler6800();
        var result = assembler.Assemble(source, sourceName: "fcc.asm");
        var segment = result.Segments[0];

        await Assert.That(segment.StartAddress).IsEqualTo((ushort)0x0200);
        await Assert.That(segment.Data.SequenceEqual(new byte[] { 0x48, 0x49 })).IsTrue();
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

    [Test]
    public async Task Assemble_HeroCompleteInstructionSetFixture_Succeeds()
    {
        var root = FindRepoRoot();
        var asmPath = Path.Combine(root, "tests", "corpus", "hero-complete-instruction-set", "hero_complete_instruction_set.asm");

        var assembler = new Assembler6800();
        var result = assembler.Assemble(File.ReadLines(asmPath), sourceName: asmPath);
        var records = assembler.AssembleToS19Records(File.ReadLines(asmPath), sourceName: asmPath);

        await Assert.That(result.Segments.Count).IsGreaterThan(0);
        await Assert.That(records.Count).IsGreaterThan(1);
    }

    [Test]
    public async Task Assemble_CrasmStyleScodeSource_MatchesExpectedS19()
    {
        var source = new[]
        {
            "cpu 6800",
            "* = $1000",
            "output scode",
            "code",
            "ldx #PHRASE",
            "stx $0303",
            "jsr $8000",
            "rts",
            "PHRASE DB $1B, $02, $23",
            "DB $18, $23, $35",
            "DB $37, $03, $2D",
            "DB $3A, $2B, $18",
            "DB $1E, $3E",
            "DB $FF",
            "clra",
            "jsr $ec3c",
            "rts",
            "ldaa #$aa",
            "jsr $ec3c",
            "rts",
            "code",
            "end"
        };

        var expected = new[]
        {
            "S1131000CE100AFF0303BD8000391B0223182335C9",
            "S113101037032D3A2B181E3EFF4FBDEC3C3986AAF0",
            "S1071020BDEC3C39AA",
            "S9030000FC"
        };

        var assembler = new Assembler6800();
        var actual = assembler.AssembleToS19Records(source, sourceName: "crasm.asm");

        await Assert.That(actual.SequenceEqual(expected)).IsTrue();
    }

    [Test]
    public async Task Assemble_CrasmCopy6800Style_MatchesReferenceS19()
    {
        var source = new[]
        {
            "cpu 6800",
            "* = $8000",
            "begin  = $40",
            "dest   = $42",
            "len    = $44",
            "ldx  #$4000",
            "stx  begin",
            "ldx  #$1430",
            "stx  len",
            "ldx  #$6000",
            "stx  dest",
            "jsr  copy",
            "wai",
            "code",
            "copy ldx begin",
            "sts begin",
            "txs",
            "ldx dest",
            "ldab len+1",
            "ldaa len",
            "addb dest+1",
            "adca dest",
            "stab dest+1",
            "staa dest",
            ".1 cpx dest",
            "beq .2",
            "pula",
            "staa 0,x",
            "inx",
            "bra .1",
            ".2 tsx",
            "lds begin",
            "stx begin",
            "clr len",
            "clr len+1",
            "rts",
            "code"
        };

        var expected = new[]
        {
            "S1138000CE4000DF40CE1430DF44CE6000DF42BDFE",
            "S113801080133EDE409F4035DE42D6459644DB4326",
            "S11380209942D74397429C42270632A7000820F67C",
            "S10F8030309E40DF407F00447F00453953",
            "S9030000FC"
        };

        var assembler = new Assembler6800();
        var actual = assembler.AssembleToS19Records(source, sourceName: "copy.6800.asm");

        await Assert.That(actual.SequenceEqual(expected)).IsTrue();
    }

    [Test]
    public async Task Assemble_CrasmMemcpy6800Style_MatchesReferenceS19()
    {
        var source = new[]
        {
            "page 66,264",
            "cpu 6800",
            "* = $1000",
            "cnt dw $0000",
            "src dw $0000",
            "dst dw $0000",
            "memcpy ldab cnt+1",
            "beq check",
            "loop ldx src",
            "ldaa ,x",
            "inx",
            "stx src",
            "ldx dst",
            "staa ,x",
            "inx",
            "stx dst",
            "decb",
            "bne loop",
            "stab cnt+1",
            "check tst cnt+0",
            "beq done",
            "dec cnt+0",
            "bra loop",
            "done rts"
        };

        var expected = new[]
        {
            "S1131000000000000000F610012718FE1002A600E0",
            "S113101008FF1002FE1004A70008FF10045A26EB74",
            "S1111020F710017D100027057A100020DE393C",
            "S9030000FC"
        };

        var assembler = new Assembler6800();
        var actual = assembler.AssembleToS19Records(source, sourceName: "memcpy.6800.asm");

        await Assert.That(actual.SequenceEqual(expected)).IsTrue();
    }

    [Test]
    public async Task Assemble_CrasmAscAndDs_EmitsExpectedBytes()
    {
        var source = new[]
        {
            "cpu 6800",
            "* = $1000",
            "nam testprog",
            "title \"Test\"",
            "asc \"HI\\0\"",
            "ds 2,$FF",
            "end"
        };

        var expected = new[]
        {
            "S1081000484900FFFF58",
            "S9030000FC"
        };

        var assembler = new Assembler6800();
        var actual = assembler.AssembleToS19Records(source, sourceName: "crasm-asc-ds.asm");

        await Assert.That(actual.SequenceEqual(expected)).IsTrue();
    }

    [Test]
    public async Task Assemble_AcceptsCommonAliasMnemonicsForARegister()
    {
        var source = new[]
        {
            "ORG $0200",
            "LDA #$12",
            "ORA #$01",
            "STA $10",
            "END"
        };

        var assembler = new Assembler6800();
        var result = assembler.Assemble(source, sourceName: "aliases.asm");
        var segment = result.Segments[0];

        await Assert.That(segment.Data.SequenceEqual(new byte[] { 0x86, 0x12, 0x8A, 0x01, 0x97, 0x10 })).IsTrue();
    }

    [Test]
    public async Task Assemble_DbSupportsQuotedStringItems()
    {
        var source = new[]
        {
            "ORG $1000",
            "DB \"HI\",',',0",
            "END"
        };

        var assembler = new Assembler6800();
        var result = assembler.Assemble(source, sourceName: "db-string.asm");
        var segment = result.Segments[0];

        await Assert.That(segment.Data.SequenceEqual(new byte[] { 0x48, 0x49, 0x2C, 0x00 })).IsTrue();
    }

    [Test]
    public async Task Assemble_RelaxesOutOfRangeBsrToJsrExtended()
    {
        var source = new[]
        {
            "ORG $0100",
            "BSR far_target",
            "RMB $90",
            "far_target RTS",
            "END"
        };

        var assembler = new Assembler6800();
        var result = assembler.Assemble(source, sourceName: "bsr-relax.asm");
        var segment = result.Segments.Single(s => s.StartAddress == 0x0100);

        await Assert.That(segment.Data.Take(3).SequenceEqual(new byte[] { 0xBD, 0x01, 0x93 })).IsTrue();
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




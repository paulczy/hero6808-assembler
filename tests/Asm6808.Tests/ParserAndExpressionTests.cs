using Asm6808.Core.Expressions;
using Asm6808.Core.Parsing;

namespace Asm6808.Tests;

public class ParserAndExpressionTests
{
    [Test]
    public async Task LineParser_ParsesLabelDirectiveWithoutColon()
    {
        var parsed = LineParser.Parse("CLRDIS   EQU $f65b  ; Clear display", 27);

        await Assert.That(parsed is not null).IsTrue();
        await Assert.That(parsed!.Label).IsEqualTo("CLRDIS");
        await Assert.That(parsed.Mnemonic).IsEqualTo("EQU");
        await Assert.That(parsed.OperandText).IsEqualTo("$f65b");
    }

    [Test]
    public async Task LineParser_ParsesOpcodeWithImmediateCharPrefix()
    {
        var parsed = LineParser.Parse("    cmpa    #'G      ; compare header", 69);

        await Assert.That(parsed is not null).IsTrue();
        await Assert.That(parsed!.Label is null).IsTrue();
        await Assert.That(parsed.Mnemonic).IsEqualTo("CMPA");
        await Assert.That(parsed.OperandText).IsEqualTo("#'G");
    }

    [Test]
    public async Task LineParser_IgnoresCommentOnlyLine()
    {
        var parsed = LineParser.Parse("; comment only", 1);
        await Assert.That(parsed is null).IsTrue();
    }

    [Test]
    public async Task LineParser_IgnoresStarCommentLine()
    {
        var parsed = LineParser.Parse("   * comment using motorola style", 2);
        await Assert.That(parsed is null).IsTrue();
    }

    [Test]
    public async Task LineParser_ParsesLabelWithoutColon()
    {
        var parsed = LineParser.Parse("waitloop    dex", 10);
        await Assert.That(parsed is not null).IsTrue();
        await Assert.That(parsed!.Label).IsEqualTo("waitloop");
        await Assert.That(parsed.Mnemonic).IsEqualTo("DEX");
        await Assert.That(parsed.OperandText).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ExpressionEvaluator_EvaluatesHexDecimalCharAndSymbolMath()
    {
        var symbols = new Dictionary<string, int>
        {
            ["DispChar"] = 0x0400
        };

        await Assert.That(ExpressionEvaluator.TryEvaluate("$10+2-1", symbols, out var simple, out _)).IsTrue();
        await Assert.That(simple).IsEqualTo(0x11);

        await Assert.That(ExpressionEvaluator.TryEvaluate("'G", symbols, out var ch, out _)).IsTrue();
        await Assert.That(ch).IsEqualTo((int)'G');

        await Assert.That(ExpressionEvaluator.TryEvaluate("DispChar+1", symbols, out var sym, out _)).IsTrue();
        await Assert.That(sym).IsEqualTo(0x0401);
    }

    [Test]
    public async Task ExpressionEvaluator_ReportsUnresolvedSymbol()
    {
        var ok = ExpressionEvaluator.TryEvaluate("MissingLabel+1", new Dictionary<string, int>(), out _, out var unresolved);

        await Assert.That(ok).IsFalse();
        await Assert.That(unresolved).IsEqualTo("MissingLabel");
    }
}


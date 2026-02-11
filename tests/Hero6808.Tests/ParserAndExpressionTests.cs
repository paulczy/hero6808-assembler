using Hero6808.Core.Expressions;
using Hero6808.Core.Parsing;

namespace Hero6808.Tests;

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
    public async Task LineParser_ParsesIndentedLabelWhenFollowedByKnownMnemonic()
    {
        var parsed = LineParser.Parse("  l1   clr 0,x", 11);
        await Assert.That(parsed is not null).IsTrue();
        await Assert.That(parsed!.Label).IsEqualTo("l1");
        await Assert.That(parsed.Mnemonic).IsEqualTo("CLR");
        await Assert.That(parsed.OperandText).IsEqualTo("0,x");
    }

    [Test]
    public async Task LineParser_ParsesLabelEqualsAsSetDirective()
    {
        var parsed = LineParser.Parse("begin = $40", 3);
        await Assert.That(parsed is not null).IsTrue();
        await Assert.That(parsed!.Label).IsEqualTo("begin");
        await Assert.That(parsed.Mnemonic).IsEqualTo("SET");
        await Assert.That(parsed.OperandText).IsEqualTo("$40");
    }

    [Test]
    public async Task LineParser_ParsesSingleTokenKnownMnemonicWithoutIndent()
    {
        var parsed = LineParser.Parse("PSHX", 11);
        await Assert.That(parsed is not null).IsTrue();
        await Assert.That(parsed!.Label is null).IsTrue();
        await Assert.That(parsed.Mnemonic).IsEqualTo("PSHX");
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

    [Test]
    public async Task ExpressionEvaluator_ResolvesDottedSymbols()
    {
        var symbols = new Dictionary<string, int>
        {
            [".1"] = 0x1234
        };

        var ok = ExpressionEvaluator.TryEvaluate(".1+1", symbols, out var value, out var unresolved);

        await Assert.That(ok).IsTrue();
        await Assert.That(unresolved).IsNull();
        await Assert.That(value).IsEqualTo(0x1235);
    }

    [Test]
    public async Task ExpressionEvaluator_SupportsUnaryBitwiseNotAndCurrentAddress()
    {
        var symbols = new Dictionary<string, int>
        {
            ["FALSE"] = 0
        };

        await Assert.That(ExpressionEvaluator.TryEvaluate("~FALSE", symbols, out var inv, out _, currentAddress: null)).IsTrue();
        await Assert.That(inv).IsEqualTo(-1);

        await Assert.That(ExpressionEvaluator.TryEvaluate("$2400-*", symbols, out var delta, out _, currentAddress: 0x2300)).IsTrue();
        await Assert.That(delta).IsEqualTo(0x0100);
    }
}



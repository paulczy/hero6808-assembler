using Hero6808.Core.Assembly;
using Hero6808.Core.SRecords;

namespace Hero6808.Tests;

public class S19WriterTests
{
    [Test]
    public async Task WriteRecords_ProducesExpectedSingleRecordAndTerminator()
    {
        byte[] payload =
        [
            0x83, 0xBD, 0xFE, 0xD3, 0x0E, 0xCE, 0x04, 0x00,
            0xBD, 0x03, 0xE4, 0xBD, 0xF6, 0x5B, 0xBD, 0xF7,
            0xE5, 0x5E, 0x37, 0x01, 0x30, 0x7E, 0xFE, 0x83,
            0xBD, 0xFE, 0xD3, 0x0E, 0xCE, 0x04, 0x1A, 0xBD
        ];

        var records = S19Writer.WriteRecords(0x0200, payload, dataBytesPerRecord: 32);

        await Assert.That(records.Count).IsEqualTo(2);
        await Assert.That(records[0]).IsEqualTo(
            "S123020083BDFED30ECE0400BD03E4BDF65BBDF7E55E3701307EFE83BDFED30ECE041ABD94");
        await Assert.That(records[1]).IsEqualTo("S9030000FC");
    }

    [Test]
    public async Task WriteRecords_SplitsPayloadAcrossMultipleS1Records()
    {
        var payload = Enumerable.Range(0, 40).Select(v => (byte)v).ToArray();

        var records = S19Writer.WriteRecords(0x1000, payload, dataBytesPerRecord: 16, executionAddress: 0x1234);

        await Assert.That(records.Count).IsEqualTo(4); // 3 data records + 1 S9
        await Assert.That(records[0].StartsWith("S1131000")).IsTrue();
        await Assert.That(records[1].StartsWith("S1131010")).IsTrue();
        await Assert.That(records[2].StartsWith("S10B1020")).IsTrue();
        await Assert.That(records[3]).IsEqualTo("S9031234B6");
    }

    [Test]
    public async Task WriteRecords_ThrowsOnInvalidDataBytesPerRecord()
    {
        var threwLow = false;
        var threwHigh = false;

        try
        {
            S19Writer.WriteRecords(0x0000, [0x01], 0);
        }
        catch (ArgumentOutOfRangeException)
        {
            threwLow = true;
        }

        try
        {
            S19Writer.WriteRecords(0x0000, [0x01], 253);
        }
        catch (ArgumentOutOfRangeException)
        {
            threwHigh = true;
        }

        await Assert.That(threwLow).IsTrue();
        await Assert.That(threwHigh).IsTrue();
    }

    [Test]
    public async Task WriteRecords_HandlesMultipleSegments()
    {
        var segments = new[]
        {
            new AddressedBytes(0x0200, [0x01, 0x02]),
            new AddressedBytes(0x0100, [0x03, 0x04])
        };

        var records = S19Writer.WriteRecords(segments);

        // Should have 2 S1 data records + 1 S9 terminator
        await Assert.That(records.Count).IsEqualTo(3);
        // Records should be sorted by address (0x0100 before 0x0200)
        await Assert.That(records[0].StartsWith("S10501000304")).IsTrue();
        await Assert.That(records[1].StartsWith("S10502000102")).IsTrue();
        await Assert.That(records[2].StartsWith("S9")).IsTrue();
    }

    [Test]
    public async Task WriteRecords_HandlesEmptySegmentList()
    {
        var records = S19Writer.WriteRecords([]);

        // Should have just the S9 terminator
        await Assert.That(records.Count).IsEqualTo(1);
        await Assert.That(records[0].StartsWith("S9")).IsTrue();
    }

    [Test]
    public async Task WriteRecords_ThrowsWhenSegmentCrosses16BitAddressSpace()
    {
        var threw = false;

        try
        {
            S19Writer.WriteRecords([new AddressedBytes(0xFFFF, [0x01, 0x02])]);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        await Assert.That(threw).IsTrue();
    }
}


using System.Text;
using Asm6808.Core.Assembly;

namespace Asm6808.Core.SRecords;

public static class S19Writer
{
    public static IReadOnlyList<string> WriteRecords(
        IEnumerable<AddressedBytes> segments,
        int dataBytesPerRecord = 32,
        ushort executionAddress = 0x0000)
    {
        if (segments is null)
        {
            throw new ArgumentNullException(nameof(segments));
        }

        if (dataBytesPerRecord < 1 || dataBytesPerRecord > 252)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dataBytesPerRecord),
                "S-record data bytes per record must be between 1 and 252.");
        }

        var records = new List<string>();
        foreach (var segment in segments.OrderBy(s => s.StartAddress))
        {
            var address = segment.StartAddress;
            var data = segment.Data.AsSpan();
            var index = 0;
            while (index < data.Length)
            {
                var count = Math.Min(dataBytesPerRecord, data.Length - index);
                records.Add(FormatS1Record(address, data.Slice(index, count)));
                address = (ushort)(address + count);
                index += count;
            }
        }

        records.Add(FormatS9Record(executionAddress));
        return records;
    }

    public static IReadOnlyList<string> WriteRecords(
        ushort startAddress,
        ReadOnlySpan<byte> data,
        int dataBytesPerRecord = 32,
        ushort executionAddress = 0x0000)
    {
        return WriteRecords([new AddressedBytes(startAddress, data.ToArray())], dataBytesPerRecord, executionAddress);
    }

    public static string WriteText(
        ushort startAddress,
        ReadOnlySpan<byte> data,
        int dataBytesPerRecord = 32,
        ushort executionAddress = 0x0000)
    {
        var records = WriteRecords(startAddress, data, dataBytesPerRecord, executionAddress);
        return string.Join(Environment.NewLine, records) + Environment.NewLine;
    }

    private static string FormatS1Record(ushort address, ReadOnlySpan<byte> data)
    {
        var count = data.Length + 3; // 2 address bytes + checksum
        var checksum = ComputeChecksum((byte)count, address, data);

        var sb = new StringBuilder(2 + 2 + 4 + (data.Length * 2) + 2);
        sb.Append("S1");
        sb.Append(count.ToString("X2"));
        sb.Append(address.ToString("X4"));
        foreach (var b in data)
        {
            sb.Append(b.ToString("X2"));
        }

        sb.Append(checksum.ToString("X2"));
        return sb.ToString();
    }

    private static string FormatS9Record(ushort executionAddress)
    {
        const byte count = 3; // 2 address bytes + checksum
        var checksum = ComputeChecksum(count, executionAddress, ReadOnlySpan<byte>.Empty);
        return $"S9{count:X2}{executionAddress:X4}{checksum:X2}";
    }

    private static byte ComputeChecksum(byte count, ushort address, ReadOnlySpan<byte> data)
    {
        var sum = count + ((address >> 8) & 0xFF) + (address & 0xFF);
        foreach (var b in data)
        {
            sum += b;
        }

        return (byte)(~sum & 0xFF);
    }
}


using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Arcv;

internal sealed class ArcvReader(byte[] data)
{
    public IReadOnlyList<ArcvEntry> Read()
    {
        Ensure(ArcvFormat.HeaderSize);
        var reader = new BufferReader(data)
        {
            ErrorFactory = static message => new ArcvException(message),
        };
        if (!reader.ReadSpan(ArcvFormat.MagicBytes.Length).SequenceEqual(ArcvFormat.MagicBytes))
        {
            throw new ArcvException(LiarUtil.Core.Strings.ARCVFileHeaderIncorrect);
        }

        var count = reader.ReadInt32();
        if (count < 0)
        {
            throw new ArcvException(string.Format(LiarUtil.Core.Strings.ARCVFileCountInvalid0, count));
        }

        Ensure(ArcvFormat.HeaderSize + (long)count * ArcvFormat.EntrySize);
        reader.Position = ArcvFormat.HeaderSize;
        var entries = new List<ArcvEntry>(count);
        for (var index = 0; index < count; index++)
        {
            var offset = reader.ReadInt32();
            var size = reader.ReadInt32();
            var checksum = reader.ReadUInt32();
            if (offset < 0 || size < 0 || offset > data.Length - size)
            {
                throw new ArcvException(string.Format(LiarUtil.Core.Strings.ARCVEntryOutOfRangeOffset0Length, offset, size));
            }

            entries.Add(new ArcvEntry(offset, size, checksum));
        }

        return entries;
    }

    public byte[] ReadContent(ArcvEntry entry) => data.AsSpan(entry.Offset, entry.Size).ToArray();

    private void Ensure(long count)
    {
        if (data.LongLength < count)
        {
            throw new ArcvException(string.Format(LiarUtil.Core.Strings.ARCVDataIncompleteExpected0BytesGot1, count, data.LongLength));
        }
    }
}

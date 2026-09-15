using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Pak.Models;

namespace LiarUtil.Core.Pak.Binary;

internal static class PakFileTable
{
    public static void Write(BufferWriter writer, PakArchive archive)
    {
        writer.WriteInt32(PakFormat.NormalMagic);
        writer.WriteInt32(PakFormat.Version);
        foreach (var entry in archive.Entries)
        {
            writer.WriteUInt8(0x00);
            WriteName(writer, entry.Name);
            writer.WriteInt32(entry.CompressedSize);
            if (archive.HasCompressionSize)
            {
                writer.WriteInt32(entry.UncompressedSize);
            }
            writer.WriteInt64(entry.FileTime);
        }

        writer.WriteUInt8(PakFormat.InfoEnd);
    }

    public static PakArchive Read(BufferReader reader)
    {
        reader.ErrorFactory = static message => new PakException(message);
        reader.ReadInt32();
        reader.ReadInt32();
        var firstEntry = reader.Position;
        var preferred = ProbeHasCompressionSize(reader, firstEntry);
        foreach (var hasCompressionSize in Candidates(preferred))
        {
            var entries = TryReadEntries(reader, firstEntry, hasCompressionSize);
            if (entries is null)
            {
                continue;
            }

            return new PakArchive
            {
                Entries = entries,
                HasCompressionSize = entries.Count > 0 && hasCompressionSize,
            };
        }

        throw new PakException(LiarUtil.Core.Strings.CannotParsePAKFileTable);
    }

    public static bool UsesUnixSeparator(IReadOnlyList<PakEntry> entries) =>
        entries.Any(entry => entry.Name.Contains('/'));

    private static IEnumerable<bool> Candidates(bool preferred)
    {
        yield return preferred;
        yield return !preferred;
    }

    private static bool ProbeHasCompressionSize(BufferReader reader, int firstEntry)
    {
        try
        {
            reader.Position = firstEntry + PakBinaryLayout.EntryFlagSize;
            ReadName(reader);
            var nameEnd = reader.Position;
            var withSize = MatchesRecordBoundary(reader, nameEnd, PakBinaryLayout.CompressedRecordTail);
            var withoutSize = MatchesRecordBoundary(reader, nameEnd, PakBinaryLayout.PlainRecordTail);
            return withSize || !withoutSize;
        }
        catch (PakException)
        {
            return false;
        }
        finally
        {
            reader.Position = firstEntry;
        }
    }

    private static bool MatchesRecordBoundary(BufferReader reader, int nameEnd, int tailSize)
    {
        var position = nameEnd + tailSize;
        if (position >= reader.Length)
        {
            return false;
        }

        reader.Position = position;
        return reader.PeekUInt8() is 0x00 or PakFormat.InfoEnd;
    }

    private static List<PakEntry>? TryReadEntries(BufferReader reader, int firstEntry, bool hasCompressionSize)
    {
        try
        {
            var entries = ReadEntries(reader, firstEntry, hasCompressionSize);
            var payload = entries.Sum(entry => (long)entry.CompressedSize);
            return payload <= reader.Length - reader.Position ? entries : null;
        }
        catch (PakException)
        {
            return null;
        }
    }

    private static List<PakEntry> ReadEntries(BufferReader reader, int firstEntry, bool hasCompressionSize)
    {
        reader.Position = firstEntry;
        var entries = new List<PakEntry>();
        while (true)
        {
            if (reader.Remaining < PakBinaryLayout.EntryFlagSize)
            {
                throw new PakException(LiarUtil.Core.Strings.PAKFileTableTruncated);
            }

            if (reader.ReadUInt8() == PakFormat.InfoEnd)
            {
                return entries;
            }

            entries.Add(ReadEntry(reader, hasCompressionSize, entries.Count));
        }
    }

    private static PakEntry ReadEntry(BufferReader reader, bool hasCompressionSize, int index)
    {
        var name = ReadName(reader);
        var compressedSize = reader.ReadInt32();
        var uncompressedSize = hasCompressionSize ? reader.ReadInt32() : 0;
        var fileTime = reader.ReadInt64();
        if (compressedSize < 0 || uncompressedSize < 0 || name.Length == 0)
        {
            throw new PakException(string.Format(LiarUtil.Core.Strings.PAKFileTableEntry0Illegal1, index, name));
        }

        return new PakEntry
        {
            Name = name,
            CompressedSize = compressedSize,
            UncompressedSize = uncompressedSize,
            FileTime = fileTime,
        };
    }

    private static string ReadName(BufferReader reader)
    {
        var length = reader.ReadUInt8();
        return length == 0 ? "" : TextCodec.DecodeLenient(reader.ReadSpan(length), TextFormat.Latin1);
    }

    private static void WriteName(BufferWriter writer, string value)
    {
        var bytes = TextCodec.EncodeMinimal(value, TextFormat.Utf8);
        if (bytes.Length > byte.MaxValue)
        {
            throw new PakException(string.Format(LiarUtil.Core.Strings.PAKFileNameTooLong0, value));
        }

        writer.WriteUInt8((byte)bytes.Length);
        writer.WriteBytes(bytes);
    }
}

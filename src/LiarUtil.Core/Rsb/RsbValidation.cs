using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Core.Compression;

namespace LiarUtil.Core.Rsb;

internal static class RsbValidation
{
    public static Func<string, Exception> ErrorFactory { get; } = static message => new InvalidDataException(message);

    public static void Range(int length, long offset, long count, string field)
    {
        if (offset < 0 || count < 0 || offset > length || count > length - offset)
        {
            throw new InvalidDataException($"RSB {field}: range {offset}+{count} exceeds {length}");
        }
    }

    public static string ResourcePath(string directory, string name)
    {
        var normalized = name.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Contains(':') || normalized.StartsWith('/') ||
            normalized.Split('/').Any(part => part is ".." or "." or ""))
        {
            throw new InvalidDataException($"Invalid RSB resource path: {name}");
        }

        return Path.Combine(directory, normalized.Replace('/', Path.DirectorySeparatorChar));
    }

    public static byte[] ReadSection(byte[] data, long offset, uint storedSize, uint size, bool compressed)
    {
        Range(data.Length, offset, storedSize, "section");
        if (size > int.MaxValue)
        {
            throw new InvalidDataException("RSB section exceeds the supported in-memory size");
        }

        if (!compressed)
        {
            if (storedSize != size)
            {
                throw new InvalidDataException("RSB uncompressed section length mismatch");
            }

            return data.AsSpan((int)offset, (int)size).ToArray();
        }

        if (storedSize == 0)
        {
            if (size == 0)
            {
                return [];
            }

            throw new EndOfStreamException();
        }

        return ZlibCodec.DecompressVerified(data.AsSpan((int)offset, checked((int)storedSize)), (int)size);
    }

    public static byte[] Unwrap(byte[] raw, out bool wrapped)
    {
        Range(raw.Length, 0, 4, "magic");
        wrapped = new BufferReader(raw).ReadInt32() == RsbConstants.SmfMagic;
        if (!wrapped)
        {
            return raw;
        }

        Range(raw.Length, 0, 8, "SMF header");
        return ReadSection(raw, 8, (uint)(raw.Length - 8), new BufferReader(raw, 4, 4).ReadUInt32(), true);
    }

    public static bool DetectEndian(byte[] data)
    {
        Range(data.Length, 0, 4, "magic");
        return new BufferReader(data).ReadInt32() switch
        {
            RsbConstants.RsbMagic => false,
            RsbConstants.RsbMagicBigEndian => true,
            _ => throw new InvalidDataException("Invalid RSB magic marker"),
        };
    }

    public static void Validate(RsbHeadInfo head, int length)
    {
        if (head.SubgroupInfoSize != head.Version.SubgroupRecordSize() ||
            head.GroupInfoSize != head.Version.GroupRecordSize() ||
            head.PoolInfoSize != RsbConstants.PoolRecordSize)
        {
            throw new InvalidDataException($"Unsupported RSB record layout for version {(int)head.Version}");
        }

        if (head.TextureInfoSize is not (RsbConstants.TextureRecordSize or RsbConstants.TextureRecordSizeAlpha or RsbConstants.TextureRecordSizeAlphaScale) ||
            head.Version == RsbVersion.V1 && head.TextureInfoSize != RsbConstants.TextureRecordSize)
        {
            throw new InvalidDataException($"Unsupported RSB texture record size {head.TextureInfoSize}");
        }

        Range(length, 0, head.ContentLength, "content");
        Range(length, head.ContentLength, RsbConstants.RsgpFileListBegin, "packet header");
        Range((int)head.ContentLength, head.SubgroupInfoBegin, (long)head.SubgroupCount * head.SubgroupInfoSize, "subgroups");
        Range((int)head.ContentLength, head.GroupInfoBegin, (long)head.GroupCount * head.GroupInfoSize, "groups");
        Range((int)head.ContentLength, head.PoolInfoBegin, (long)head.PoolCount * head.PoolInfoSize, "pools");
        Range((int)head.ContentLength, head.TextureInfoBegin, (long)head.TextureCount * head.TextureInfoSize, "textures");
        Range((int)head.ContentLength, head.ResourcePathBegin, head.ResourcePathLength, "resource index");
        Range((int)head.ContentLength, head.SubgroupNameBegin, head.SubgroupNameLength, "subgroup index");
        Range((int)head.ContentLength, head.GroupNameBegin, head.GroupNameLength, "group index");
    }
}

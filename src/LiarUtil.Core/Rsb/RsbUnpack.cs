using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Rsb.Definitions;
using LiarUtil.Core.Rsb.Resources;

namespace LiarUtil.Core.Rsb;

internal sealed record RsbUnpackOptions
{
    public required string InputPath { get; init; }

    public required string OutputFolder { get; init; }

    public required RsbVersion Version { get; init; }

    public bool ExportResources { get; init; } = true;

    public bool WriteTextureHeader { get; init; } = true;

    public bool ConvertImages { get; init; }

    public bool DeleteAfterConvert { get; init; }

    public string PtxFormat { get; init; } = "ARGB";
}

internal static class RsbUnpack
{
    public static void Unpack(RsbUnpackOptions options)
    {
        var data = RsbValidation.Unwrap(File.ReadAllBytes(options.InputPath), out var zlibAll);
        var bigEndian = RsbValidation.DetectEndian(data);
        var reader = new BufferReader(data, bigEndian);
        reader.ErrorFactory = RsbValidation.ErrorFactory;
        var head = RsbHeadInfo.Read(reader);
        if (head.Version != options.Version)
        {
            throw new InvalidDataException(
                $"The RSB file is version {(int)head.Version} but version {(int)options.Version} was selected");
        }

        RsbValidation.Validate(head, data.Length);

        var groups = ReadTable(reader, head.GroupInfoBegin, head.GroupCount, item => RsbGroupInfo.Read(item, head.Version));
        var subgroups = ReadTable(reader, head.SubgroupInfoBegin, head.SubgroupCount, item => RsbSubgroupInfo.Read(item, head.Version));
        var pools = ReadTable(reader, head.PoolInfoBegin, head.PoolCount, item => RsbPoolInfo.Read(item));
        var textures = ReadTable(reader, head.TextureInfoBegin, head.TextureCount, item => RsbTextureInfo.Read(item, head.TextureInfoSize));

        var specialPool = UsesExplicitPools(subgroups, pools);
        var definition = new RsbDefinition
        {
            BigEndian = bigEndian,
            CompressionLevel = DetectCompressionLevel(data, subgroups),
            WholeFileCompressed = zlibAll,
            SpecialPool = specialPool,
            TextureRecordSize = head.TextureInfoSize == RsbConstants.TextureRecordSize ? null : head.TextureInfoSize,
            Pools = specialPool ? BuildPools(pools) : new List<RsbDefinitionPool>(),
            Groups = BuildGroups(groups),
            Subgroups = BuildSubgroups(subgroups),
            ResourceDefinitions = ReadResourceDefinitions(data, head, bigEndian),
        };

        RsbPacketExtractor.Extract(data, head, subgroups, pools, textures, definition, options);
        RsbDefinitionSerializer.Save(options.OutputFolder, definition);
    }

    private static List<T> ReadTable<T>(BufferReader reader, uint offset, uint count, Func<BufferReader, T> read)
    {
        reader.Position = checked((int)offset);
        var result = new List<T>(checked((int)count));
        for (var i = 0u; i < count; i++)
        {
            result.Add(read(reader));
        }

        return result;
    }

    private static bool UsesExplicitPools(IReadOnlyList<RsbSubgroupInfo> subgroups, IReadOnlyList<RsbPoolInfo> pools)
    {
        if (subgroups.Count != pools.Count)
        {
            return true;
        }

        for (var i = 0; i < subgroups.Count; i++)
        {
            if (subgroups[i].PoolIndex != i || pools[i].InstanceCount != 1)
            {
                return true;
            }
        }

        return false;
    }

    private static List<RsbDefinitionPool> BuildPools(IReadOnlyList<RsbPoolInfo> pools) =>
        [.. pools.Select(pool => new RsbDefinitionPool
        {
            Identifier = pool.Identifier,
            InstanceCount = pool.InstanceCount,
        })];

    private static List<RsbDefinitionGroup> BuildGroups(IReadOnlyList<RsbGroupInfo> groups)
    {
        var result = new List<RsbDefinitionGroup>(groups.Count);
        foreach (var group in groups)
        {
            var definition = new RsbDefinitionGroup { Identifier = group.Identifier };
            for (var i = 0; i < group.SubgroupCount; i++)
            {
                definition.Subgroups.Add(new RsbDefinitionSubgroupReference
                {
                    Index = group.Subgroups[i].Index,
                    Resolution = group.Subgroups[i].Resolution,
                    Locale = group.Subgroups[i].Locale,
                });
            }

            result.Add(definition);
        }

        return result;
    }

    private static List<RsbDefinitionSubgroup> BuildSubgroups(IReadOnlyList<RsbSubgroupInfo> subgroups) =>
        [.. subgroups.Select(info => new RsbDefinitionSubgroup
        {
            Identifier = info.Identifier,
            Pool = info.PoolIndex,
            CompressGeneral = (info.Compression & 0b10) != 0,
            CompressTexture = (info.Compression & 0b01) != 0,
        })];

    private static string DetectCompressionLevel(byte[] data, IReadOnlyList<RsbSubgroupInfo> subgroups)
    {
        foreach (var info in subgroups)
        {
            var offset = (info.Compression & 0b10) != 0 && info.GeneralCompressedSize >= 2
                ? (long)info.Offset + info.GeneralOffset
                : (info.Compression & 0b01) != 0 && info.TextureCompressedSize >= 2
                    ? (long)info.Offset + info.TextureOffset
                    : -1;
            if (offset >= 0 && offset + 1 < data.Length)
            {
                return RsbCompressionLevel.FromZlibHeader(data[(int)offset + 1]);
            }
        }

        return RsbCompressionLevel.Optimal;
    }

    private static List<RsbResourceDefinition> ReadResourceDefinitions(byte[] data, RsbHeadInfo head, bool bigEndian)
    {
        if (head.ResourceDefinitionBegin == 0 || head.ContentLength <= head.ResourceDefinitionBegin || head.ContentLength > data.Length)
        {
            return [];
        }

        var size = (int)(head.ContentLength - head.ResourceDefinitionBegin);
        var dat = BuildResourceDefinitionData(data.AsSpan((int)head.ResourceDefinitionBegin, size), head, bigEndian);
        return RsbResourceDefinitionCodec.Parse(dat, bigEndian);
    }

    private static byte[] BuildResourceDefinitionData(ReadOnlySpan<byte> section, RsbHeadInfo head, bool bigEndian)
    {
        if (head.ResourceDefinitionBegin > head.ResourceDefinitionDataBegin ||
            head.ResourceDefinitionDataBegin > head.ResourceDefinitionStringBegin ||
            head.ResourceDefinitionStringBegin >= head.ContentLength)
        {
            throw new InvalidDataException("Invalid RSB resource definition offsets");
        }

        var writer = new BufferWriter(bigEndian);
        writer.WriteInt32(RsbConstants.ResourceDefinitionMagic);
        writer.WriteInt32(1);
        writer.WriteInt32(0x14);
        writer.WriteUInt32(head.ResourceDefinitionDataBegin - head.ResourceDefinitionBegin + 0x14);
        writer.WriteUInt32(head.ResourceDefinitionStringBegin - head.ResourceDefinitionBegin + 0x14);
        writer.WriteBytes(section);
        return writer.ToArray();
    }
}

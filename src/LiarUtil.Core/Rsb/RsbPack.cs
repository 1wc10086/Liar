using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Core.Compression;
using LiarUtil.Core.Core.IO;
using LiarUtil.Core.Rsb.Definitions;
using LiarUtil.Core.Rsb.Resources;
using LiarUtil.Core.Texture;

namespace LiarUtil.Core.Rsb;

internal sealed record RsbPackOptions
{
    public required string InputFolder { get; init; }

    public required string OutputPath { get; init; }

    public required RsbVersion Version { get; init; }

    public string PtxFormat { get; init; } = "ARGB";
}

internal static class RsbPack
{
    public static void Pack(RsbPackOptions options)
    {
        var definition = RsbDefinitionSerializer.Load(options.InputFolder);
        var version = options.Version;
        var bigEndian = definition.BigEndian;
        var context = new RsgpPackContext(
            Path.Combine(options.InputFolder, RsbLayout.ResourceFolderName),
            bigEndian,
            RsbCompressionLevel.ToZlibLevel(RsbCompressionLevel.Parse(definition.CompressionLevel)),
            options.PtxFormat);

        var groups = BuildGroups(definition);
        var subgroups = new List<RsbSubgroupInfo>(definition.Subgroups.Count);
        var built = new List<RsgpBuildResult>(definition.Subgroups.Count);
        var textures = new List<RsbTextureInfo>();
        var packets = Path.Combine(options.InputFolder, RsbLayout.PacketFolderName);
        foreach (var subgroup in definition.Subgroups)
        {
            var info = new RsbSubgroupInfo
            {
                Identifier = subgroup.Identifier,
                PoolIndex = definition.SpecialPool ? subgroup.Pool : (uint)subgroups.Count,
                Compression = (subgroup.CompressGeneral ? 0b10u : 0u) | (subgroup.CompressTexture ? 0b01u : 0u),
            };
            var result = LoadPacket(subgroup, packets, context, version) ?? RsgpPacketBuilder.Build(subgroup, context);
            info.TextureBegin = (uint)textures.Count;
            info.TextureCount = result.TextureCount;
            textures.AddRange(result.Textures);
            subgroups.Add(info);
            built.Add(result);
        }

        var pools = BuildPools(definition, subgroups);
        var writer = new BufferWriter(bigEndian);
        var headerSize = version.HeaderSize();
        writer.Resize(headerSize, 0);
        writer.Position = headerSize;

        var head = new RsbHeadInfo
        {
            Version = version,
            SubgroupCount = (uint)subgroups.Count,
            SubgroupInfoSize = version.SubgroupRecordSize(),
            GroupCount = (uint)groups.Count,
            GroupInfoSize = version.GroupRecordSize(),
            PoolCount = (uint)pools.Count,
            PoolInfoSize = RsbConstants.PoolRecordSize,
            TextureCount = (uint)textures.Count,
            TextureInfoSize = ResolveTextureRecordSize(definition, version),
        };

        head.ResourcePathBegin = (uint)writer.Position;
        var resourcePath = WriteIndex(writer, BuildResourcePathIndex(definition), bigEndian);
        head.ResourcePathLength = (uint)resourcePath;

        head.SubgroupNameBegin = (uint)writer.Position;
        head.SubgroupNameLength = (uint)WriteIndex(writer, BuildSubgroupNameIndex(subgroups), bigEndian);

        head.GroupInfoBegin = (uint)writer.Position;
        foreach (var group in groups)
        {
            group.Write(writer, version);
        }

        head.GroupNameBegin = (uint)writer.Position;
        head.GroupNameLength = (uint)WriteIndex(writer, BuildGroupNameIndex(definition), bigEndian);

        head.SubgroupInfoBegin = (uint)writer.Position;
        var subgroupInfoEnd = (int)(head.SubgroupInfoBegin + head.SubgroupInfoSize * (uint)subgroups.Count);
        writer.Resize(subgroupInfoEnd, 0);
        writer.Position = subgroupInfoEnd;

        head.PoolInfoBegin = (uint)writer.Position;
        foreach (var pool in pools)
        {
            pool.Write(writer);
        }

        head.TextureInfoBegin = (uint)writer.Position;
        foreach (var texture in textures)
        {
            texture.Write(writer, head.TextureInfoSize);
        }

        if (definition.ResourceDefinitions.Count > 0)
        {
            if (bigEndian)
            {
                AlignWriter(writer);
            }

            var dat = RsbResourceDefinitionCodec.Encode(definition.ResourceDefinitions, bigEndian);
            head.ResourceDefinitionBegin = (uint)writer.Position;
            head.ResourceDefinitionDataBegin = head.ResourceDefinitionBegin + ReadUInt32(dat, 12, bigEndian) - ReadUInt32(dat, 8, bigEndian);
            head.ResourceDefinitionStringBegin = head.ResourceDefinitionBegin + ReadUInt32(dat, 16, bigEndian) - ReadUInt32(dat, 8, bigEndian);
            writer.WriteBytes(dat.AsSpan(0x14));
        }

        AlignWriter(writer);
        head.ContentLength = (uint)writer.Position;

        for (var i = 0; i < subgroups.Count; i++)
        {
            subgroups[i].Offset = (uint)writer.Position;
            RsgpPacketBuilder.Write(writer, built[i], subgroups[i], version, bigEndian);
            var pool = pools[(int)subgroups[i].PoolIndex];
            var decompressed = subgroups[i].GeneralOffset + subgroups[i].GeneralSize;
            pool.TextureDataOffset = definition.SpecialPool ? Math.Max(pool.TextureDataOffset, decompressed) : decompressed;
            pool.TextureDataSize = definition.SpecialPool ? Math.Max(pool.TextureDataSize, subgroups[i].TextureSize) : subgroups[i].TextureSize;
            if (!version.HasSubgroupTextureRange())
            {
                pool.TextureBegin = subgroups[i].TextureBegin;
                pool.TextureCount = subgroups[i].TextureCount;
            }
        }

        writer.Position = (int)head.SubgroupInfoBegin;
        foreach (var info in subgroups)
        {
            info.Write(writer, version);
        }

        writer.Position = (int)head.PoolInfoBegin;
        foreach (var pool in pools)
        {
            pool.Write(writer);
        }

        writer.Position = 0;
        head.Write(writer);
        FileIO.WriteAllBytes(options.OutputPath, definition.WholeFileCompressed ? WrapSmf(writer) : writer.ToArray());
    }

    private static int WriteIndex(BufferWriter writer, CompressStringList index, bool bigEndian)
    {
        var bytes = index.Write(bigEndian);
        writer.WriteBytes(bytes);
        return bytes.Length;
    }

    private static List<RsbGroupInfo> BuildGroups(RsbDefinition definition)
    {
        var result = new List<RsbGroupInfo>(definition.Groups.Count);
        foreach (var group in definition.Groups)
        {
            var info = new RsbGroupInfo
            {
                Identifier = group.Identifier,
                SubgroupCount = (uint)group.Subgroups.Count,
            };
            if (info.SubgroupCount > RsbGroupInfo.MaxSubgroups)
            {
                throw new InvalidDataException($"RSB group '{group.Identifier}' references more than 64 subgroups");
            }

            for (var i = 0; i < group.Subgroups.Count; i++)
            {
                var reference = group.Subgroups[i];
                if (reference.Index >= definition.Subgroups.Count)
                {
                    throw new InvalidDataException($"RSB group '{group.Identifier}' references an unknown subgroup");
                }

                info.Subgroups[i].Index = reference.Index;
                info.Subgroups[i].Resolution = reference.Resolution;
                info.Subgroups[i].Locale = reference.Locale;
            }

            result.Add(info);
        }

        return result;
    }

    private static List<RsbPoolInfo> BuildPools(RsbDefinition definition, IReadOnlyList<RsbSubgroupInfo> subgroups)
    {
        if (!definition.SpecialPool)
        {
            return [.. subgroups.Select(subgroup => new RsbPoolInfo { Identifier = subgroup.Identifier + "_AutoPool" })];
        }

        var pools = new List<RsbPoolInfo>(definition.Pools.Count);
        foreach (var pool in definition.Pools)
        {
            pools.Add(new RsbPoolInfo { Identifier = pool.Identifier, InstanceCount = pool.InstanceCount });
        }

        if (pools.Count == 0)
        {
            throw new InvalidDataException("RSB definition uses explicit pools but declares none");
        }

        foreach (var subgroup in subgroups)
        {
            if (subgroup.PoolIndex >= pools.Count)
            {
                throw new InvalidDataException($"RSB subgroup '{subgroup.Identifier}' references an unknown pool");
            }
        }

        return pools;
    }

    private static RsgpBuildResult? LoadPacket(
        RsbDefinitionSubgroup subgroup,
        string packetRoot,
        RsgpPackContext context,
        RsbVersion version)
    {
        if (string.IsNullOrWhiteSpace(subgroup.Packet))
        {
            return null;
        }

        var path = Path.Combine(packetRoot, subgroup.Packet);
        if (!File.Exists(path))
        {
            return null;
        }

        var data = File.ReadAllBytes(path);
        var packet = RsgpPacket.Read(data, context.BigEndian);
        var head = packet.Head;
        if (head.Version != (int)version)
        {
            throw new InvalidDataException(
                $"Packet '{subgroup.Packet}' is version {head.Version} but version {(int)version} was selected");
        }

        RsbValidation.Range(data.Length, head.FileListBegin, head.FileListLength, "file list");
        RsbValidation.Range(data.Length, head.GeneralOffset, head.GeneralCompressedSize, "general section");
        RsbValidation.Range(data.Length, head.TextureOffset, head.TextureCompressedSize, "texture section");

        var metadata = new Dictionary<string, RsbDefinitionResource>(StringComparer.OrdinalIgnoreCase);
        foreach (var resource in subgroup.Resources)
        {
            metadata[resource.Path] = resource;
        }

        var textures = new List<RsbTextureInfo>();
        var count = 0u;
        for (var i = 0; i < packet.Count; i++)
        {
            var entry = packet[i];
            if (entry.Type != 2)
            {
                continue;
            }

            if (!metadata.TryGetValue(entry.Name, out var resource))
            {
                throw new InvalidDataException($"Packet '{subgroup.Packet}' contains '{entry.Name}' which is missing from the definition");
            }

            textures.Add(new RsbTextureInfo
            {
                Width = entry.Width,
                Height = entry.Height,
                Pitch = resource.Pitch,
                Format = resource.Format,
                AdditionalByteCount = resource.AdditionalByteCount,
                Scale = resource.Scale,
            });
            count++;
        }

        return new RsgpBuildResult
        {
            Identifier = subgroup.Identifier,
            GeneralSize = head.GeneralSize,
            TextureSize = head.TextureSize,
            GeneralCompressedSize = head.GeneralCompressedSize,
            TextureCompressedSize = head.TextureCompressedSize,
            TextureCount = count,
            GeneralData = data.AsSpan((int)head.GeneralOffset, (int)head.GeneralCompressedSize).ToArray(),
            TextureData = data.AsSpan((int)head.TextureOffset, (int)head.TextureCompressedSize).ToArray(),
            FileList = packet.FileListData,
            Textures = textures,
        };
    }

    private static CompressStringList BuildResourcePathIndex(RsbDefinition definition)
    {
        var result = new CompressStringList(0);
        for (var i = 0; i < definition.Subgroups.Count; i++)
        {
            foreach (var resource in definition.Subgroups[i].Resources)
            {
                result.Add(new CompressString(resource.Path, 0, index: (uint)i));
            }
        }

        return result;
    }

    private static CompressStringList BuildSubgroupNameIndex(IReadOnlyList<RsbSubgroupInfo> subgroups)
    {
        var result = new CompressStringList(0);
        for (var i = 0; i < subgroups.Count; i++)
        {
            result.Add(new CompressString(subgroups[i].Identifier.ToUpperInvariant(), 0, index: (uint)i));
        }

        return result;
    }

    private static CompressStringList BuildGroupNameIndex(RsbDefinition definition)
    {
        var result = new CompressStringList(0);
        for (var i = 0; i < definition.Groups.Count; i++)
        {
            result.Add(new CompressString(definition.Groups[i].Identifier.ToUpperInvariant(), 0, index: (uint)i));
        }

        return result;
    }

    private static uint ResolveTextureRecordSize(RsbDefinition definition, RsbVersion version)
    {
        if (version == RsbVersion.V1)
        {
            return RsbConstants.TextureRecordSize;
        }

        if (definition.TextureRecordSize is not { } size)
        {
            return RsbConstants.TextureRecordSize;
        }

        return size switch
        {
            RsbConstants.TextureRecordSize or RsbConstants.TextureRecordSizeAlpha or RsbConstants.TextureRecordSizeAlphaScale => size,
            _ => throw new InvalidDataException($"Unsupported texture record size {size}"),
        };
    }

    private static uint ReadUInt32(byte[] data, int offset, bool bigEndian) =>
        new BufferReader(data, offset, sizeof(uint)) { BigEndian = bigEndian }.ReadUInt32();

    private static void AlignWriter(BufferWriter writer)
    {
        var aligned = (int)RsbConstants.AlignTo4K(writer.Position);
        writer.Resize(aligned, 0);
        writer.Position = aligned;
    }

    private static byte[] WrapSmf(BufferWriter writer)
    {
        var compressed = ZlibCodec.Compress(writer.ToArray(), 9);
        var output = new BufferWriter(8 + compressed.Length);
        output.WriteInt32(RsbConstants.SmfMagic);
        output.WriteInt32(writer.Length);
        output.WriteBytes(compressed);
        return output.ToArray();
    }
}

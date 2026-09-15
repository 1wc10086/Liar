using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Rsb.Definitions;
using LiarUtil.Core.Texture;

namespace LiarUtil.Core.Rsb;

internal static class RsbPacketExtractor
{
    public static void Extract(
        byte[] data,
        RsbHeadInfo head,
        IReadOnlyList<RsbSubgroupInfo> subgroups,
        IReadOnlyList<RsbPoolInfo> pools,
        IReadOnlyList<RsbTextureInfo> textures,
        RsbDefinition definition,
        RsbUnpackOptions options)
    {
        var resourceRoot = Path.Combine(options.OutputFolder, RsbLayout.ResourceFolderName);
        var packetRoot = Path.Combine(options.OutputFolder, RsbLayout.PacketFolderName);
        var names = new PacketNames();
        var bigEndian = definition.BigEndian;
        for (var i = 0; i < subgroups.Count; i++)
        {
            var info = subgroups[i];
            try
            {
                ExtractSubgroup(
                    data, head, info, pools, textures, definition.Subgroups[i], resourceRoot, packetRoot, names, options, bigEndian);
            }
            catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException or OverflowException)
            {
                throw new InvalidDataException($"RSB subgroup '{info.Identifier}': {ex.Message}", ex);
            }
        }
    }

    private static void ExtractSubgroup(
        byte[] data,
        RsbHeadInfo head,
        RsbSubgroupInfo info,
        IReadOnlyList<RsbPoolInfo> pools,
        IReadOnlyList<RsbTextureInfo> textures,
        RsbDefinitionSubgroup subgroup,
        string resourceRoot,
        string packetRoot,
        PacketNames names,
        RsbUnpackOptions options,
        bool bigEndian)
    {
        if (info.Offset < head.ContentLength || info.Size < RsbConstants.RsgpFileListBegin)
        {
            throw new InvalidDataException("RSGP overlaps the RSB header or has a truncated header");
        }

        RsbValidation.Range(data.Length, info.Offset, info.Size, "rsgp");
        var reader = new BufferReader(data, bigEndian);
        reader.ErrorFactory = RsbValidation.ErrorFactory;
        reader.Position = checked((int)info.Offset);
        var packet = RsgpPacket.Read(reader);
        var packetHead = packet.Head;
        RsbValidation.Range(checked((int)info.Size), packetHead.FileListBegin, packetHead.FileListLength, "file list");
        RsbValidation.Range((int)info.Size, packetHead.GeneralOffset, packetHead.GeneralCompressedSize, "general section");
        RsbValidation.Range((int)info.Size, packetHead.TextureOffset, packetHead.TextureCompressedSize, "texture section");
        if (packetHead.FileListBegin < RsbConstants.RsgpFileListBegin ||
            packetHead.GeneralOffset < (long)packetHead.FileListBegin + packetHead.FileListLength ||
            packetHead.TextureOffset < (long)packetHead.GeneralOffset + packetHead.GeneralCompressedSize)
        {
            throw new InvalidDataException("Overlapping RSGP sections");
        }

        if (packetHead.Flags != info.Compression)
        {
            throw new InvalidDataException("RSGP compression flags mismatch");
        }

        if (info.PoolIndex >= pools.Count)
        {
            throw new InvalidDataException("RSGP pool index out of range");
        }

        var pool = pools[(int)info.PoolIndex];
        var textureBegin = head.Version.HasSubgroupTextureRange() ? info.TextureBegin : pool.TextureBegin;
        if (!options.ExportResources)
        {
            var name = names.Allocate(info.Identifier);
            Directory.CreateDirectory(packetRoot);
            File.WriteAllBytes(Path.Combine(packetRoot, name), data.AsSpan((int)info.Offset, (int)info.Size).ToArray());
            subgroup.Packet = name;
            FillResources(subgroup, packet, textures, textureBegin);
            return;
        }

        var general = RsbValidation.ReadSection(
            data, (long)info.Offset + packetHead.GeneralOffset, packetHead.GeneralCompressedSize, packetHead.GeneralSize, (packetHead.Flags & 0b10) != 0);
        var texture = RsbValidation.ReadSection(
            data, (long)info.Offset + packetHead.TextureOffset, packetHead.TextureCompressedSize, packetHead.TextureSize, (packetHead.Flags & 0b01) != 0);

        for (var i = 0; i < packet.Count; i++)
        {
            var entry = packet[i];
            var target = RsbValidation.ResourcePath(resourceRoot, entry.Name);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            var section = entry.Type == 1 ? general : texture;
            RsbValidation.Range(section.Length, entry.Offset, entry.Size, entry.Name);
            if (entry.Type == 1)
            {
                File.WriteAllBytes(target, section.AsSpan((int)entry.Offset, (int)entry.Size).ToArray());
                subgroup.Resources.Add(new RsbDefinitionResource { Kind = RsbResourceKind.General, Path = entry.Name });
                continue;
            }

            if (entry.Type != 2)
            {
                throw new InvalidDataException($"Unknown RSGP resource type {entry.Type} for {entry.Name}");
            }

            var meta = ResolveTexture(textures, textureBegin, entry.Index2, entry.Name);
            if (meta.Width != entry.Width || meta.Height != entry.Height)
            {
                throw new InvalidDataException($"Texture size mismatch for {entry.Name}");
            }

            var payload = section.AsSpan((int)entry.Offset, (int)entry.Size);
            if (options.WriteTextureHeader)
            {
                WriteTexture(target, meta, payload, bigEndian);
            }
            else
            {
                File.WriteAllBytes(target, payload.ToArray());
            }

            if (options.ConvertImages)
            {
                ConvertTexture(target, meta, payload, bigEndian, options);
                if (options.DeleteAfterConvert)
                {
                    File.Delete(target);
                }
            }

            subgroup.Resources.Add(Describe(entry.Name, meta));
        }
    }

    private static void FillResources(
        RsbDefinitionSubgroup subgroup,
        RsgpPacket packet,
        IReadOnlyList<RsbTextureInfo> textures,
        uint textureBegin)
    {
        for (var i = 0; i < packet.Count; i++)
        {
            var entry = packet[i];
            if (entry.Type == 1)
            {
                subgroup.Resources.Add(new RsbDefinitionResource { Kind = RsbResourceKind.General, Path = entry.Name });
                continue;
            }

            if (entry.Type != 2)
            {
                throw new InvalidDataException($"Unknown RSGP resource type {entry.Type} for {entry.Name}");
            }

            subgroup.Resources.Add(Describe(entry.Name, ResolveTexture(textures, textureBegin, entry.Index2, entry.Name)));
        }
    }

    private static RsbTextureInfo ResolveTexture(IReadOnlyList<RsbTextureInfo> textures, uint textureBegin, uint index, string name)
    {
        var resolved = (long)textureBegin + index;
        if (resolved >= textures.Count)
        {
            throw new InvalidDataException($"Invalid texture index for {name}");
        }

        return textures[(int)resolved];
    }

    private static RsbDefinitionResource Describe(string path, RsbTextureInfo meta) => new()
    {
        Kind = RsbResourceKind.Texture,
        Path = path,
        Width = meta.Width,
        Height = meta.Height,
        Pitch = meta.Pitch,
        Format = meta.Format,
        AdditionalByteCount = meta.AdditionalByteCount,
        Scale = meta.Scale,
    };

    private static void WriteTexture(string path, RsbTextureInfo texture, ReadOnlySpan<byte> payload, bool bigEndian)
    {
        var header = new BufferWriter(bigEndian);
        header.WriteUInt32(Ptx.Magic);
        header.WriteUInt32(Ptx.Version);
        texture.Write(header, RsbConstants.TextureRecordSizeAlphaScale);
        using var output = File.Create(path);
        output.Write(header.ToArray());
        output.Write(payload);
    }

    private static void ConvertTexture(
        string target,
        RsbTextureInfo texture,
        ReadOnlySpan<byte> payload,
        bool bigEndian,
        RsbUnpackOptions options)
    {
        var source = target;
        string? temporary = null;
        if (!options.WriteTextureHeader)
        {
            temporary = target + ".decode.ptx";
            WriteTexture(temporary, texture, payload, bigEndian);
            source = temporary;
        }

        try
        {
            Ptx.Decode(source, Path.ChangeExtension(target, ".png"), true, "", 0, 0, options.PtxFormat);
        }
        finally
        {
            if (temporary is not null && File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private sealed class PacketNames
    {
        private readonly HashSet<string> _used = new(StringComparer.OrdinalIgnoreCase);

        public string Allocate(string identifier)
        {
            var stem = RsbText.SanitizeFileName(identifier);
            var name = stem + RsbLayout.PacketExtension;
            var index = 1;
            while (!_used.Add(name))
            {
                name = $"{stem}_{index}{RsbLayout.PacketExtension}";
                index++;
            }

            return name;
        }
    }
}

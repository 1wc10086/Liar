using System.Globalization;
using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Core.Compression;
using LiarUtil.Core.Rsb.Definitions;
using LiarUtil.Core.Texture;

namespace LiarUtil.Core.Rsb;

internal sealed record RsgpPackContext(string ResourceRoot, bool BigEndian, int CompressionLevel, string PtxFormat);

internal static class RsgpPacketBuilder
{
    private const int PtxHeaderSize = 32;
    private const uint PtxMagicLittleEndian = 0x70747831;
    private const uint PtxMagicBigEndian = 0x31787470;

    public static RsgpBuildResult Build(RsbDefinitionSubgroup subgroup, RsgpPackContext context)
    {
        var entries = Collect(subgroup, context);
        var (generalSize, textureSize) = AssignOffsets(entries);
        var general = new byte[checked((int)generalSize)];
        var texture = new byte[checked((int)textureSize)];
        Fill(entries, general, texture);
        return Assemble(subgroup, entries, general, texture, context);
    }

    private static List<RsgpEntry> Collect(RsbDefinitionSubgroup subgroup, RsgpPackContext context)
    {
        var entries = new List<RsgpEntry>(subgroup.Resources.Count);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var resource in subgroup.Resources)
        {
            var kind = RsbResourceKind.Parse(resource.Kind);
            var path = RsbValidation.ResourcePath(context.ResourceRoot, resource.Path);
            if (!paths.Add(path))
            {
                throw new InvalidDataException($"Duplicate resource path in {subgroup.Identifier}: {resource.Path}");
            }

            entries.Add(kind == RsbResourceKind.Texture
                ? BuildTextureEntry(resource, path, context)
                : BuildGeneralEntry(resource, path));
        }

        return entries;
    }

    private static RsgpEntry BuildGeneralEntry(RsbDefinitionResource resource, string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidDataException("File not found: " + path);
        }

        return new RsgpEntry
        {
            Path = resource.Path,
            IsTexture = false,
            Data = File.ReadAllBytes(path),
        };
    }

    private static RsgpEntry BuildTextureEntry(RsbDefinitionResource resource, string path, RsgpPackContext context)
    {
        var raw = ReadTextureBytes(resource, path, context);
        var texture = new RsbTextureInfo();
        byte[] payload;
        if (raw.Length >= PtxHeaderSize && IsPtxHeader(raw))
        {
            texture = ReadPtxHeader(raw);
            payload = raw[PtxHeaderSize..];
        }
        else
        {
            texture.Width = resource.Width;
            texture.Height = resource.Height;
            texture.Pitch = resource.Pitch;
            texture.Format = resource.Format;
            texture.AdditionalByteCount = resource.AdditionalByteCount;
            texture.Scale = resource.Scale;
            if (texture.Width == 0 || texture.Height == 0)
            {
                throw new InvalidDataException(
                    $"Texture size missing for {resource.Path}; re-unpack with the texture header enabled or provide a PTX file");
            }

            payload = raw;
        }

        return new RsgpEntry
        {
            Path = resource.Path,
            IsTexture = true,
            Data = payload,
            Texture = texture,
        };
    }

    private static byte[] ReadTextureBytes(RsbDefinitionResource resource, string path, RsgpPackContext context)
    {
        if (File.Exists(path))
        {
            return File.ReadAllBytes(path);
        }

        var png = FindPng(path);
        if (png is null)
        {
            throw new InvalidDataException("File not found: " + path);
        }

        var format = resource.Format;
        var name = (context.BigEndian, format) switch
        {
            (true, 0) => "ARGB8888_BE",
            (true, 5) => "DXT5_RGBA_BE",
            _ => format.ToString(CultureInfo.InvariantCulture),
        };
        return Ptx.Encode(png, name, true, context.PtxFormat);
    }

    private static string? FindPng(string path)
    {
        var upper = Path.ChangeExtension(path, ".PNG");
        if (File.Exists(upper))
        {
            return upper;
        }

        var lower = Path.ChangeExtension(path, ".png");
        return File.Exists(lower) ? lower : null;
    }

    private static bool IsPtxHeader(ReadOnlySpan<byte> data)
    {
        var magic = new BufferReader(data).ReadUInt32();
        if (magic is not (PtxMagicLittleEndian or PtxMagicBigEndian))
        {
            return false;
        }

        var bigEndian = magic == PtxMagicBigEndian;
        return ReadUInt32(data, 4, bigEndian) == 1;
    }

    private static RsbTextureInfo ReadPtxHeader(ReadOnlySpan<byte> data)
    {
        var bigEndian = new BufferReader(data).ReadUInt32() == PtxMagicBigEndian;
        return new RsbTextureInfo
        {
            Width = ReadUInt32(data, 8, bigEndian),
            Height = ReadUInt32(data, 12, bigEndian),
            Pitch = ReadUInt32(data, 16, bigEndian),
            Format = ReadUInt32(data, 20, bigEndian),
            AdditionalByteCount = ReadUInt32(data, 24, bigEndian),
            Scale = ReadUInt32(data, 28, bigEndian),
        };
    }

    private static (long General, long Texture) AssignOffsets(List<RsgpEntry> entries)
    {
        long general = 0;
        long texture = 0;
        foreach (var entry in entries)
        {
            if (entry.IsTexture)
            {
                entry.Offset = texture;
                texture = RsbConstants.AlignTo4K(texture + entry.Data.Length);
            }
            else
            {
                entry.Offset = general;
                general = RsbConstants.AlignTo4K(general + entry.Data.Length);
            }
        }

        return (general, texture);
    }

    private static void Fill(List<RsgpEntry> entries, byte[] general, byte[] texture)
    {
        foreach (var entry in entries)
        {
            if (entry.Data.Length == 0)
            {
                continue;
            }

            entry.Data.CopyTo(entry.IsTexture ? texture : general, (int)entry.Offset);
        }
    }

    private static RsgpBuildResult Assemble(
        RsbDefinitionSubgroup subgroup,
        List<RsgpEntry> entries,
        byte[] general,
        byte[] texture,
        RsgpPackContext context)
    {
        var fileList = new CompressStringList(1);
        var textures = new List<RsbTextureInfo>();
        var paths = new List<string>(entries.Count);
        var textureIndex = 0u;
        foreach (var entry in entries)
        {
            paths.Add(entry.Path);
            if (entry.IsTexture)
            {
                fileList.Add(new CompressString(entry.Path, 2, offset: (uint)entry.Offset, size: (uint)entry.Data.Length)
                {
                    Index2 = textureIndex,
                    Width = entry.Texture.Width,
                    Height = entry.Texture.Height,
                });
                textures.Add(entry.Texture);
                textureIndex++;
            }
            else
            {
                fileList.Add(new CompressString(entry.Path, 1, offset: (uint)entry.Offset, size: (uint)entry.Data.Length));
            }
        }

        var generalData = Pack(general, subgroup.CompressGeneral, context.CompressionLevel);
        var textureData = Pack(texture, subgroup.CompressTexture, context.CompressionLevel);
        return new RsgpBuildResult
        {
            Identifier = subgroup.Identifier,
            GeneralSize = (uint)general.Length,
            TextureSize = (uint)texture.Length,
            GeneralCompressedSize = (uint)generalData.Length,
            TextureCompressedSize = (uint)textureData.Length,
            TextureCount = textureIndex,
            GeneralData = generalData,
            TextureData = textureData,
            FileList = fileList.Write(context.BigEndian),
            Textures = textures,
            Paths = paths,
        };
    }

    private static byte[] Pack(byte[] data, bool compress, int level)
    {
        if (!compress)
        {
            return data;
        }

        var compressed = ZlibCodec.Compress(data, level);
        var aligned = (int)RsbConstants.AlignTo4K(compressed.Length);
        if (aligned == compressed.Length)
        {
            return compressed;
        }

        var result = new byte[aligned];
        compressed.CopyTo(result, 0);
        return result;
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset, bool bigEndian) =>
        new BufferReader(data[offset..].ToArray(), bigEndian).ReadUInt32();

    public static int Write(BufferWriter writer, RsgpBuildResult result, RsbSubgroupInfo info, RsbVersion version, bool bigEndian)
    {
        var head = new RsgpHeadInfo
        {
            Version = (int)version,
            Flags = info.Compression,
            GeneralSize = result.GeneralSize,
            TextureSize = result.TextureSize,
            GeneralCompressedSize = result.GeneralCompressedSize,
            TextureCompressedSize = result.TextureCompressedSize,
            FileListLength = (uint)result.FileList.Length,
            FileListBegin = RsbConstants.RsgpFileListBegin,
        };

        var packet = new BufferWriter(bigEndian);
        packet.Position = 0;
        packet.Resize((int)RsbConstants.RsgpFileListBegin, 0);
        packet.Position = (int)RsbConstants.RsgpFileListBegin;
        packet.WriteBytes(result.FileList);
        var aligned = (int)RsbConstants.AlignTo4K(packet.Position);
        packet.Resize(aligned, 0);
        packet.Position = aligned;
        var generalOffset = aligned;
        packet.WriteBytes(result.GeneralData);
        var textureOffset = packet.Position;
        packet.WriteBytes(result.TextureData);

        head.FileOffset = (uint)generalOffset;
        head.GeneralOffset = (uint)generalOffset;
        head.TextureOffset = (uint)textureOffset;

        var bytes = packet.ToArray();
        var headerWriter = new BufferWriter(bigEndian);
        head.Write(headerWriter);
        var headerBytes = headerWriter.ToArray();
        Array.Copy(headerBytes, bytes, headerBytes.Length);

        info.ContentOffset = (uint)generalOffset;
        info.GeneralOffset = (uint)generalOffset;
        info.GeneralCompressedSize = result.GeneralCompressedSize;
        info.GeneralSize = result.GeneralSize;
        info.GeneralPoolSize = result.GeneralSize;
        info.TextureOffset = (uint)textureOffset;
        info.TextureCompressedSize = result.TextureCompressedSize;
        info.TextureSize = result.TextureSize;
        info.TexturePoolSize = 0;
        info.Size = (uint)bytes.Length;
        writer.WriteBytes(bytes);
        return bytes.Length;
    }

    private sealed class RsgpEntry
    {
        public string Path = "";
        public bool IsTexture;
        public byte[] Data = [];
        public long Offset;
        public RsbTextureInfo Texture = new();
    }
}

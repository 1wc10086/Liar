using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Core.IO;
using LiarUtil.Core.Pak.Binary;
using LiarUtil.Core.Pak.Definitions;
using LiarUtil.Core.Pak.Models;
using LiarUtil.Core.Pak.Packing;

namespace LiarUtil.Core.Pak;

internal sealed record PakUnpackOptions
{
    public required string InputPath { get; init; }

    public required string OutputFolder { get; init; }
}

internal static class PakUnpack
{
    public static void Unpack(PakUnpackOptions options)
    {
        var payload = PakPayloadDecoder.Decode(File.ReadAllBytes(options.InputPath));
        if (payload.TvVersion)
        {
            UnpackTvZip(options);
            return;
        }

        var reader = new BufferReader(payload.Data);
        var archive = PakFileTable.Read(reader);
        var definition = CreateDefinition(payload, archive);
        var resourceRoot = Path.Combine(options.OutputFolder, definition.ResourceFolder);
        Directory.CreateDirectory(resourceRoot);

        foreach (var entry in archive.Entries)
        {
            if (!definition.PcEncrypted)
            {
                definition.Xbox360PtxAlign |= PakPadding.Read(reader, out _);
            }

            var localPath = PakPath.ToLocal(resourceRoot, entry.Name);
            var stored = reader.ReadBytes(entry.CompressedSize);
            var body = entry.IsCompressed ? PakZlib.Decompress(stored, entry.UncompressedSize) : stored;
            FileIO.WriteAllBytes(localPath, body);
            definition.Files.Add(new PakFileDefinition
            {
                Path = PakPath.Normalize(entry.Name),
                FileTime = entry.FileTime,
                Compression = entry.IsCompressed ? PakCompression.Zlib : PakCompression.Store,
            });
        }

        definition.InferCompressionRules();
        PakDefinitionSerializer.Save(options.OutputFolder, definition);
    }

    private static void UnpackTvZip(PakUnpackOptions options)
    {
        var definition = new PakDefinition { TvVersion = true, Files = [] };
        PakTvZip.Unpack(options.InputPath, Path.Combine(options.OutputFolder, definition.ResourceFolder));
        PakDefinitionSerializer.Save(options.OutputFolder, definition);
    }

    private static PakDefinition CreateDefinition(PakPayload payload, PakArchive archive) => new()
    {
        PcEncrypted = payload.PcEncrypted,
        TvVersion = payload.TvVersion,
        XmemCompress = payload.XmemCompressed,
        ZlibCompress = archive.HasCompressionSize,
        WindowsPathSeparate = !PakFileTable.UsesUnixSeparator(archive.Entries),
        Files = [],
    };
}

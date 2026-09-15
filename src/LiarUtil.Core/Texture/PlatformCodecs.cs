using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Core.Compression;
using LiarUtil.Core.Core.IO;
using LiarUtil.Core.Texture.Codecs;

namespace LiarUtil.Core.Texture;

internal static class PlatformCodecs
{
    private static void SaveBitmap(ImageBitmap bitmap, string path)
    {
        FileIO.EnsureDirectory(path);
        bitmap.Save(path);
    }

    public static void TextVDecode(string inputPath, string outputPath)
    {
        var data = File.ReadAllBytes(inputPath);
        var stream = new BufferReader(data);
        if (!stream.ReadBytes(8).AsSpan().SequenceEqual("SEXYTEX\0"u8))
        {
            throw new InvalidDataException("Invalid TEX TV magic");
        }

        if (stream.ReadInt32() != 0)
        {
            throw new InvalidDataException("Invalid TEX TV version");
        }

        var width = stream.ReadInt32();
        var height = stream.ReadInt32();
        var format = stream.ReadInt32();
        var flags = stream.ReadUInt32();
        stream.Skip(20);

        byte[] pixels;
        if ((flags & 1) != 0)
        {
            pixels = ZlibCodec.Decompress(stream.ReadBytes(stream.Length - stream.Position), width * height * 4);
        }
        else
        {
            pixels = stream.ReadBytes(stream.Length - stream.Position);
        }

        using var bitmap = new ImageBitmap(width, height);
        var px = bitmap.Pixels;
        var texStream = new BufferReader(pixels);
        switch (format)
        {
            case 1:
                PixelFormats.ReadL8(texStream, px, width * height);
                break;
            case 2:
                PixelFormats.ReadArgb8888(texStream, px, width * height);
                break;
            case 3:
                PixelFormats.ReadArgb4444(texStream, px, width * height);
                break;
            case 4:
                PixelFormats.ReadArgb1555(texStream, px, width * height);
                break;
            case 5:
                PixelFormats.ReadRgb565(texStream, px, width * height);
                break;
            case 6:
                PixelFormats.ReadAbgr8888(texStream, px, width * height);
                break;
            case 7:
                PixelFormats.ReadRgba4444(texStream, px, width * height);
                break;
            case 8:
                PixelFormats.ReadRgba5551(texStream, px, width * height);
                break;
            case 9:
                PixelFormats.ReadXrgb8888(texStream, px, width * height);
                break;
            case 10:
                PixelFormats.ReadLa88(texStream, px, width * height);
                break;
            default:
                throw new InvalidDataException("Unsupported TEX TV format");
        }

        SaveBitmap(bitmap, outputPath);
    }

    public static void TextVEncode(string inputPath, string outputPath, string formatName)
    {
        var z = true;
        var format = ParseTextVFormat(formatName, ref z);
        using var bitmap = ImageBitmap.Load(inputPath);
        var texWriter = new BufferWriter();
        var pixels = bitmap.Pixels;
        var count = bitmap.Size;
        switch (format)
        {
            case 1:
                PixelFormats.WriteL8(texWriter, pixels, count);
                break;
            case 2:
                PixelFormats.WriteArgb8888(texWriter, pixels, count);
                break;
            case 3:
                PixelFormats.WriteArgb4444(texWriter, pixels, count);
                break;
            case 4:
                PixelFormats.WriteArgb1555(texWriter, pixels, count);
                break;
            case 5:
                PixelFormats.WriteRgb565(texWriter, pixels, count);
                break;
            case 6:
                PixelFormats.WriteAbgr8888(texWriter, pixels, count);
                break;
            case 7:
                PixelFormats.WriteRgba4444(texWriter, pixels, count);
                break;
            case 8:
                PixelFormats.WriteRgba5551(texWriter, pixels, count);
                break;
            case 9:
                PixelFormats.WriteXrgb8888(texWriter, pixels, count);
                break;
            case 10:
                PixelFormats.WriteLa88(texWriter, pixels, count);
                break;
            default:
                throw new InvalidDataException("Unsupported TEX TV format");
        }

        var writer = new BufferWriter();
        writer.WriteBytes("SEXYTEX\0"u8);
        writer.WriteInt32(0);
        writer.WriteInt32(bitmap.Width);
        writer.WriteInt32(bitmap.Height);
        writer.WriteInt32(format);
        if (z)
        {
            var compressed = ZlibCodec.Compress(texWriter.ToArray(), 9);
            writer.WriteUInt32(1);
            writer.WriteInt32(1);
            writer.WriteInt32(compressed.Length);
            writer.WriteInt32(0);
            writer.WriteInt32(0);
            writer.WriteInt32(0);
            writer.WriteBytes(compressed);
        }
        else
        {
            writer.WriteUInt32(0);
            writer.WriteInt32(1);
            writer.WriteInt32(0);
            writer.WriteInt32(0);
            writer.WriteInt32(0);
            writer.WriteInt32(0);
            writer.WriteBytes(texWriter.ToArray());
        }

        FileIO.WriteAllBytes(outputPath, writer.ToArray());
    }

    private static int ParseTextVFormat(string format, ref bool compressed)
    {
        compressed = true;
        var baseName = format;
        if (format.Length > 4 && format.EndsWith("_RAW", StringComparison.Ordinal))
        {
            compressed = false;
            baseName = format[..^4];
        }

        var id = baseName switch
        {
            "L8" => 1,
            "ARGB8888" => 2,
            "ARGB4444" => 3,
            "ARGB1555" => 4,
            "RGB565" => 5,
            "ABGR8888" => 6,
            "RGBA4444" => 7,
            "RGBA5551" => 8,
            "XRGB8888" => 9,
            "LA88" => 10,
            _ when int.TryParse(baseName, out var value) => value >= 10 ? SetRawAndReturn(value, ref compressed) : value + 1,
            _ => 1,
        };

        return id;
    }

    private static int SetRawAndReturn(int value, ref bool compressed)
    {
        compressed = false;
        return value - 9;
    }

    public static void PtxPs3Decode(string inputPath, string outputPath)
    {
        var data = File.ReadAllBytes(inputPath);
        var stream = new BufferReader(data);
        if (!stream.ReadBytes(4).AsSpan().SequenceEqual("DDS "u8))
        {
            throw new InvalidDataException("Invalid PS3 DDS magic");
        }

        if (stream.ReadInt32() != 0x7C || stream.ReadInt32() != 528391)
        {
            throw new InvalidDataException("Invalid PS3 DDS header");
        }

        var height = stream.ReadInt32();
        var width = stream.ReadInt32();
        _ = stream.ReadInt32();
        stream.Skip(44);
        if (!stream.ReadBytes(4).AsSpan().SequenceEqual("NVTT"u8))
        {
            throw new InvalidDataException("Invalid PS3 NVTT");
        }

        stream.Skip(12);
        if (!stream.ReadBytes(4).AsSpan().SequenceEqual("DXT5"u8))
        {
            throw new InvalidDataException("Invalid PS3 DXT5");
        }

        stream.Skip(40);
        using var bitmap = new ImageBitmap(width, height);
        Dxt.DecodeDxt5(stream, width, height, bitmap.Pixels);
        SaveBitmap(bitmap, outputPath);
    }

    public static void PtxPs3Encode(string inputPath, string outputPath)
    {
        using var bitmap = ImageBitmap.Load(inputPath);
        var writer = new BufferWriter();
        writer.WriteBytes("DDS "u8);
        writer.WriteInt32(0x7C);
        writer.WriteInt32(528391);
        writer.WriteInt32(bitmap.Height);
        writer.WriteInt32(bitmap.Width);
        writer.WriteInt32(0);
        for (var i = 0; i < 11; i++)
        {
            writer.WriteInt32(0);
        }

        writer.WriteBytes("NVTT"u8);
        writer.WriteInt32(131080);
        writer.WriteInt32(32);
        writer.WriteInt32(4);
        writer.WriteBytes("DXT5"u8);
        for (var i = 0; i < 5; i++)
        {
            writer.WriteInt32(0);
        }

        writer.WriteInt32(4096);
        for (var i = 0; i < 4; i++)
        {
            writer.WriteInt32(0);
        }

        Dxt.EncodeDxt5(writer, bitmap.Width, bitmap.Height, bitmap.Pixels);
        var result = writer.ToArray();
        var size = BitConverter.GetBytes(bitmap.Width * bitmap.Height);
        result[0x14] = size[0];
        result[0x15] = size[1];
        result[0x16] = size[2];
        result[0x17] = size[3];
        FileIO.WriteAllBytes(outputPath, result);
    }

    public static void PtxPsvDecode(string inputPath, string outputPath)
    {
        var data = File.ReadAllBytes(inputPath);
        var stream = new BufferReader(data);
        if (!stream.ReadBytes(4).AsSpan().SequenceEqual("GXT\0"u8))
        {
            throw new InvalidDataException("Invalid PSV GXT magic");
        }

        if (stream.ReadInt32() != 0x10000003)
        {
            throw new InvalidDataException("Invalid PSV GXT version");
        }

        stream.Skip(48);
        var width = stream.ReadUInt16();
        var height = stream.ReadUInt16();
        stream.Skip(4);
        using var bitmap = new ImageBitmap(width, height);
        DxtMorton.DecodeDxt5Morton(stream, width, height, bitmap.Pixels);
        SaveBitmap(bitmap, outputPath);
    }

    public static void PtxPsvEncode(string inputPath, string outputPath)
    {
        using var bitmap = ImageBitmap.Load(inputPath);
        var writer = new BufferWriter();
        writer.WriteBytes("GXT\0"u8);
        writer.WriteInt32(0x10000003);
        writer.WriteInt32(1);
        writer.WriteInt32(0x40);
        var s1 = 16;
        writer.WriteInt32(0);
        writer.WriteInt32(0);
        writer.WriteInt32(0);
        writer.WriteInt32(0x40);
        var s2 = 32;
        writer.WriteInt32(0);
        writer.WriteInt32(-1);
        writer.WriteInt32(0);
        writer.WriteInt32(0);
        writer.WriteInt32(-2030043136);
        writer.WriteUInt16((ushort)bitmap.Width);
        writer.WriteUInt16((ushort)bitmap.Height);
        writer.WriteInt32(1);
        DxtMorton.EncodeDxt5Morton(writer, bitmap.Width, bitmap.Height, bitmap.Pixels);
        var bytes = writer.ToArray();
        var size = BitConverter.GetBytes(bytes.Length - 0x40);
        bytes[s1] = size[0];
        bytes[s1 + 1] = size[1];
        bytes[s1 + 2] = size[2];
        bytes[s1 + 3] = size[3];
        bytes[s2] = size[0];
        bytes[s2 + 1] = size[1];
        bytes[s2 + 2] = size[2];
        bytes[s2 + 3] = size[3];
        FileIO.WriteAllBytes(outputPath, bytes);
    }

    public static void PtxXbox360Decode(string inputPath, string outputPath)
    {
        var data = File.ReadAllBytes(inputPath);
        var stream = new BufferReader(data, ByteOrder.Big);
        stream.Position = data.Length - 16;
        var width = stream.ReadInt32();
        var height = stream.ReadInt32();
        var block = stream.ReadInt32();
        if (stream.ReadInt32() != 1409294362)
        {
            throw new InvalidDataException("Invalid Xbox360 magic");
        }

        stream.Position = 0;
        using var bitmap = new ImageBitmap(width, height);
        DxtMorton.DecodeDxt5Padding(stream, width, height, block, bitmap.Pixels);
        SaveBitmap(bitmap, outputPath);
    }

    public static void PtxXbox360Encode(string inputPath, string outputPath)
    {
        using var bitmap = ImageBitmap.Load(inputPath);
        var width = bitmap.Width;
        if (width % 128 != 0)
        {
            width = (width / 128) * 128 + 128;
        }

        var writer = new BufferWriter(ByteOrder.Big);
        DxtMorton.EncodeDxt5Padding(writer, bitmap.Width, bitmap.Height, width << 2, bitmap.Pixels);
        writer.WriteInt32(bitmap.Width);
        writer.WriteInt32(bitmap.Height);
        writer.WriteInt32(width << 2);
        writer.WriteInt32(1409294362);
        FileIO.WriteAllBytes(outputPath, writer.ToArray());
    }
}

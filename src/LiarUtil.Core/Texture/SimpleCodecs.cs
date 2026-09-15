using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Core.Compression;
using LiarUtil.Core.Core.IO;

namespace LiarUtil.Core.Texture;

internal static class SimpleCodecs
{
    private static byte[] ReadFile(string path) => File.ReadAllBytes(path);

    public static void CdatDecode(string inputPath, string outputPath, string key)
    {
        var data = ReadFile(inputPath);
        if (data.Length < 11 || !data.AsSpan(0, 11).SequenceEqual("CRYPT_RES\n\0"u8))
        {
            throw new InvalidDataException("Invalid CDAT magic");
        }

        var output = new List<byte>();
        if (data.Length >= 0x112)
        {
            for (var i = 0; i < 0x100; i++)
            {
                output.Add((byte)(data[19 + i] ^ key[i % key.Length]));
            }
        }

        var offset = data.Length >= 0x112 ? 19 + 0x100 : 19;
        output.AddRange(data.AsSpan(Math.Min(offset, data.Length)));
        FileIO.WriteAllBytes(outputPath, [.. output]);
    }

    public static void CdatEncode(string inputPath, string outputPath, string key)
    {
        var data = ReadFile(inputPath);
        var output = new List<byte>(data.Length + 19);
        output.AddRange("CRYPT_RES\n\0"u8);
        output.AddRange(BitConverter.GetBytes(data.Length));
        var count = Math.Min(0x100, data.Length);
        for (var i = 0; i < count; i++)
        {
            output.Add((byte)(data[i] ^ key[i % key.Length]));
        }

        if (data.Length >= 0x100)
        {
            output.AddRange(data.AsSpan(0x100));
        }

        FileIO.WriteAllBytes(outputPath, [.. output]);
    }

    public static void TexDecode(string inputPath, string outputPath)
    {
        var data = ReadFile(inputPath);
        var stream = new BufferReader(data);
        if (stream.ReadUInt16() != 2677)
        {
            throw new InvalidDataException("Invalid TEX magic");
        }

        var width = stream.ReadUInt16();
        var height = stream.ReadUInt16();
        var format = stream.ReadUInt16();
        using var bitmap = new ImageBitmap(width, height);
        switch (format)
        {
            case 1:
                Codecs.PixelFormats.ReadAbgr8888(stream, bitmap.Pixels, width * height);
                break;
            case 2:
                Codecs.PixelFormats.ReadRgba4444(stream, bitmap.Pixels, width * height);
                break;
            case 3:
                Codecs.PixelFormats.ReadRgba5551(stream, bitmap.Pixels, width * height);
                break;
            case 4:
                Codecs.PixelFormats.ReadRgb565(stream, bitmap.Pixels, width * height);
                break;
            default:
                throw new InvalidDataException("Unsupported TEX format");
        }

        FileIO.EnsureDirectory(outputPath);
        bitmap.Save(outputPath);
    }

    public static void TexEncode(string inputPath, string outputPath, string formatName)
    {
        var format = ParseTexFormat(formatName);
        using var bitmap = ImageBitmap.Load(inputPath);
        var writer = new BufferWriter();
        writer.WriteUInt16(2677);
        writer.WriteUInt16((ushort)bitmap.Width);
        writer.WriteUInt16((ushort)bitmap.Height);
        writer.WriteUInt16(format);
        WriteTexPixels(writer, bitmap, format);
        FileIO.WriteAllBytes(outputPath, writer.ToArray());
    }

    private static ushort ParseTexFormat(string format)
    {
        return format switch
        {
            "ABGR8888" => 1,
            "RGBA4444" => 2,
            "RGBA5551" => 3,
            "RGB565" => 4,
            _ when int.TryParse(format, out var value) => (ushort)(value + 1),
            _ => 1,
        };
    }

    private static void WriteTexPixels(BufferWriter writer, ImageBitmap bitmap, ushort format)
    {
        var pixels = bitmap.Pixels;
        var count = bitmap.Size;
        switch (format)
        {
            case 1:
                Codecs.PixelFormats.WriteAbgr8888(writer, pixels, count);
                break;
            case 2:
                Codecs.PixelFormats.WriteRgba4444(writer, pixels, count);
                break;
            case 3:
                Codecs.PixelFormats.WriteRgba5551(writer, pixels, count);
                break;
            case 4:
                Codecs.PixelFormats.WriteRgb565(writer, pixels, count);
                break;
            default:
                throw new InvalidDataException("Unsupported TEX format");
        }
    }

    public static void TxzDecode(string inputPath, string outputPath)
    {
        var data = ReadFile(inputPath);
        var stream = new BufferReader(data);
        if (stream.ReadUInt16() != 2677)
        {
            throw new InvalidDataException("Invalid TXZ magic");
        }

        var width = stream.ReadUInt16();
        var height = stream.ReadUInt16();
        var format = stream.ReadUInt16();
        var decompressed = ZlibCodec.Decompress(stream.ReadBytes(stream.Length - stream.Position));
        var texStream = new BufferReader(decompressed);
        using var bitmap = new ImageBitmap(width, height);
        switch (format)
        {
            case 1:
                Codecs.PixelFormats.ReadAbgr8888(texStream, bitmap.Pixels, width * height);
                break;
            case 2:
                Codecs.PixelFormats.ReadRgba4444(texStream, bitmap.Pixels, width * height);
                break;
            case 3:
                Codecs.PixelFormats.ReadRgba5551(texStream, bitmap.Pixels, width * height);
                break;
            case 4:
                Codecs.PixelFormats.ReadRgb565(texStream, bitmap.Pixels, width * height);
                break;
            default:
                throw new InvalidDataException("Unsupported TXZ format");
        }

        FileIO.EnsureDirectory(outputPath);
        bitmap.Save(outputPath);
    }

    public static void TxzEncode(string inputPath, string outputPath, string formatName)
    {
        var format = ParseTexFormat(formatName);
        using var bitmap = ImageBitmap.Load(inputPath);
        var texWriter = new BufferWriter();
        WriteTexPixels(texWriter, bitmap, format);
        var writer = new BufferWriter();
        writer.WriteUInt16(2677);
        writer.WriteUInt16((ushort)bitmap.Width);
        writer.WriteUInt16((ushort)bitmap.Height);
        writer.WriteUInt16(format);
        writer.WriteBytes(ZlibCodec.Compress(texWriter.ToArray(), 9));
        FileIO.WriteAllBytes(outputPath, writer.ToArray());
    }

    public static void XnbDecode(string inputPath, string outputPath)
    {
        var data = ReadFile(inputPath);
        var stream = new BufferReader(data);
        var magic = new byte[] { 0x58, 0x4E, 0x42, 0x6D, 0x05, 0x00 };
        if (!stream.ReadBytes(6).AsSpan().SequenceEqual(magic))
        {
            throw new InvalidDataException("Invalid XNB magic");
        }

        _ = stream.ReadInt32();
        var info = new byte[]
        {
            0x01, 0x94, 0x01, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74, 0x2E, 0x58, 0x6E, 0x61, 0x2E,
            0x46, 0x72, 0x61, 0x6D, 0x65, 0x77, 0x6F, 0x72, 0x6B, 0x2E, 0x43, 0x6F, 0x6E, 0x74, 0x65, 0x6E, 0x74,
            0x2E, 0x54, 0x65, 0x78, 0x74, 0x75, 0x72, 0x65, 0x32, 0x44, 0x52, 0x65, 0x61, 0x64, 0x65, 0x72, 0x2C,
            0x20, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74, 0x2E, 0x58, 0x6E, 0x61, 0x2E, 0x46, 0x72,
            0x61, 0x6D, 0x65, 0x77, 0x6F, 0x72, 0x6B, 0x2E, 0x47, 0x72, 0x61, 0x70, 0x68, 0x69, 0x63, 0x73, 0x2C,
            0x20, 0x56, 0x65, 0x72, 0x73, 0x69, 0x6F, 0x6E, 0x3D, 0x34, 0x2E, 0x30, 0x2E, 0x30, 0x2E, 0x30, 0x2C,
            0x20, 0x43, 0x75, 0x6C, 0x74, 0x75, 0x72, 0x65, 0x3D, 0x6E, 0x65, 0x75, 0x74, 0x72, 0x61, 0x6C, 0x2C,
            0x20, 0x50, 0x75, 0x62, 0x6C, 0x69, 0x63, 0x4B, 0x65, 0x79, 0x54, 0x6F, 0x6B, 0x65, 0x6E, 0x3D, 0x38,
            0x34, 0x32, 0x63, 0x66, 0x38, 0x62, 0x65, 0x31, 0x64, 0x65, 0x35, 0x30, 0x35, 0x35, 0x33, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01,
        };
        if (!stream.ReadBytes(157).AsSpan().SequenceEqual(info))
        {
            throw new InvalidDataException("Invalid XNB info");
        }

        if (stream.ReadInt32() != 0)
        {
            throw new InvalidDataException("Invalid XNB flags");
        }

        var width = stream.ReadInt32();
        var height = stream.ReadInt32();
        if (stream.ReadInt32() != 1)
        {
            throw new InvalidDataException("Invalid XNB format");
        }

        _ = stream.ReadInt32();
        using var bitmap = new ImageBitmap(width, height);
        Codecs.PixelFormats.ReadAbgr8888(stream, bitmap.Pixels, width * height);
        bitmap.Save(outputPath);
    }

    public static void XnbEncode(string inputPath, string outputPath)
    {
        using var bitmap = ImageBitmap.Load(inputPath);
        var size = bitmap.Width * bitmap.Height * 4;
        var writer = new BufferWriter();
        writer.WriteBytes([0x58, 0x4E, 0x42, 0x6D, 0x05, 0x00]);
        writer.WriteInt32(197 + size);
        var info = new byte[]
        {
            0x01, 0x94, 0x01, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74, 0x2E, 0x58, 0x6E, 0x61, 0x2E,
            0x46, 0x72, 0x61, 0x6D, 0x65, 0x77, 0x6F, 0x72, 0x6B, 0x2E, 0x43, 0x6F, 0x6E, 0x74, 0x65, 0x6E, 0x74,
            0x2E, 0x54, 0x65, 0x78, 0x74, 0x75, 0x72, 0x65, 0x32, 0x44, 0x52, 0x65, 0x61, 0x64, 0x65, 0x72, 0x2C,
            0x20, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74, 0x2E, 0x58, 0x6E, 0x61, 0x2E, 0x46, 0x72,
            0x61, 0x6D, 0x65, 0x77, 0x6F, 0x72, 0x6B, 0x2E, 0x47, 0x72, 0x61, 0x70, 0x68, 0x69, 0x63, 0x73, 0x2C,
            0x20, 0x56, 0x65, 0x72, 0x73, 0x69, 0x6F, 0x6E, 0x3D, 0x34, 0x2E, 0x30, 0x2E, 0x30, 0x2E, 0x30, 0x2C,
            0x20, 0x43, 0x75, 0x6C, 0x74, 0x75, 0x72, 0x65, 0x3D, 0x6E, 0x65, 0x75, 0x74, 0x72, 0x61, 0x6C, 0x2C,
            0x20, 0x50, 0x75, 0x62, 0x6C, 0x69, 0x63, 0x4B, 0x65, 0x79, 0x54, 0x6F, 0x6B, 0x65, 0x6E, 0x3D, 0x38,
            0x34, 0x32, 0x63, 0x66, 0x38, 0x62, 0x65, 0x31, 0x64, 0x65, 0x35, 0x30, 0x35, 0x35, 0x33, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01,
        };
        writer.WriteBytes(info);
        writer.WriteInt32(0);
        writer.WriteInt32(bitmap.Width);
        writer.WriteInt32(bitmap.Height);
        writer.WriteInt32(1);
        writer.WriteInt32(size);
        Codecs.PixelFormats.WriteAbgr8888(writer, bitmap.Pixels, bitmap.Size);
        FileIO.WriteAllBytes(outputPath, writer.ToArray());
    }
}

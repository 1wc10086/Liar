using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Core.IO;
using LiarUtil.Core.Texture.Codecs;

namespace LiarUtil.Core.Texture;

public static class Ptx
{
    internal const uint Magic = 0x70747831;
    internal const uint Version = 1;

    public static void Decode(string inputPath, string outputPath, bool useHeader, string userFormat, int userWidth, int userHeight, string fmt0Mode)
    {
        var data = File.ReadAllBytes(inputPath);
        var spec = default(PtxFormatSpec);
        var payload = Array.Empty<byte>();
        var width = 0;
        var height = 0;

        if (useHeader)
        {
            var stream = new BufferReader(data);
            var header = ReadHeader(stream, out var bigEndianHeader);
            width = (int)header.Width;
            height = (int)header.Height;
            payload = stream.ReadBytes(stream.Length - stream.Position);
            spec = SpecFromHeader(header, bigEndianHeader, payload, userFormat, fmt0Mode);
        }
        else
        {
            spec = PtxFormats.Parse(userFormat);
            if (!spec.Valid)
            {
                throw new InvalidDataException("Unknown PTX format");
            }

            width = userWidth;
            height = userHeight;
            payload = data;
        }

        using var bitmap = new ImageBitmap(width, height);
        if (PtxFormats.IsAuxFileId(spec.FileId))
        {
            var colorSize = CalcAuxColorPayloadSize(spec.FileId, width, height);
            if (colorSize > payload.Length)
            {
                throw new InvalidDataException("PTX payload truncated");
            }

            DecodeSimple(ColorOnlyTagForAux(spec), payload[..colorSize], width, height, fmt0Mode, bitmap.Pixels);
            PtxAlpha.DecodeByScheme(spec.Alpha, payload.AsSpan(colorSize), bitmap.Pixels, width * height);
        }
        else
        {
            DecodeSimple(spec.Tag, payload, width, height, fmt0Mode, bitmap.Pixels);
        }

        FileIO.EnsureDirectory(outputPath);
        bitmap.Save(outputPath);
    }

    public static byte[] Encode(string inputPath, string userFormat, bool writeHeader, string fmt0Mode)
    {
        var spec = PtxFormats.Parse(userFormat);
        if (!spec.Valid)
        {
            throw new InvalidDataException("Unknown PTX format");
        }

        using var bitmap = ImageBitmap.Load(inputPath);
        var writer = new BufferWriter();
        if (writeHeader)
        {
            for (var i = 0; i < 8; i++)
            {
                writer.WriteUInt32(0);
            }
        }

        var bigEndianHeader = spec.BigEndianHeader;
        var header = new HeaderInfo
        {
            Width = (uint)bitmap.Width,
            Height = (uint)bitmap.Height,
            Format = spec.FileId,
        };

        if (PtxFormats.IsAuxFileId(spec.FileId))
        {
            EncodeSimple(ColorOnlyTagForAux(spec), writer, bitmap, fmt0Mode);
            header.Pitch = (uint)(bitmap.Width << 2);
            var alpha = PtxAlpha.EncodeByScheme(spec.Alpha, bitmap.Pixels, bitmap.Size);
            if (alpha.Length > 0)
            {
                writer.WriteBytes(alpha);
            }

            header.AlphaSize = IsVariableAlphaSize(spec.Alpha) ? (uint)alpha.Length : 0u;
            header.AlphaFormat = ShouldWriteAlpha64(spec) ? 0x64u : 0u;
        }
        else
        {
            header.Pitch = EncodeSimple(spec.Tag, writer, bitmap, fmt0Mode);
            header.AlphaSize = 0;
            header.AlphaFormat = ShouldWriteAlpha64(spec) ? 0x64u : 0u;
        }

        var result = writer.ToArray();
        if (writeHeader)
        {
            var headerBytes = WriteHeader(header, bigEndianHeader);
            headerBytes.CopyTo(result, 0);
        }

        return result;
    }

    private sealed class HeaderInfo
    {
        public uint Width;
        public uint Height;
        public uint Pitch;
        public uint Format;
        public uint AlphaSize;
        public uint AlphaFormat;
    }

    private static HeaderInfo ReadHeader(BufferReader stream, out bool bigEndianHeader)
    {
        var header = new HeaderInfo();
        var magic = stream.ReadUInt32();
        bigEndianHeader = false;
        if (magic == 0x31787470)
        {
            stream.BigEndian = true;
            magic = Magic;
            bigEndianHeader = true;
        }
        else if (magic != Magic)
        {
            throw new InvalidDataException("Invalid PTX magic");
        }

        _ = stream.ReadUInt32();
        header.Width = stream.ReadUInt32();
        header.Height = stream.ReadUInt32();
        header.Pitch = stream.ReadUInt32();
        header.Format = stream.ReadUInt32();
        header.AlphaSize = stream.ReadUInt32();
        header.AlphaFormat = stream.ReadUInt32();
        return header;
    }

    private static byte[] WriteHeader(HeaderInfo header, bool bigEndian)
    {
        var buffer = new BufferWriter(bigEndian ? ByteOrder.Big : ByteOrder.Little);
        buffer.WriteUInt32(Magic);
        buffer.WriteUInt32(Version);
        buffer.WriteUInt32(header.Width);
        buffer.WriteUInt32(header.Height);
        buffer.WriteUInt32(header.Pitch);
        buffer.WriteUInt32(header.Format);
        buffer.WriteUInt32(header.AlphaSize);
        buffer.WriteUInt32(header.AlphaFormat);
        return buffer.ToArray();
    }

    private static PtxFormatSpec SpecFromHeader(HeaderInfo header, bool bigEndianHeader, ReadOnlySpan<byte> payload, string hintedFormat, string fmt0Mode)
    {
        var hinted = hintedFormat.Length > 0 ? PtxFormats.Parse(hintedFormat) : default;
        switch (header.Format)
        {
            case 0:
                if (bigEndianHeader)
                {
                    return hinted.Valid && hinted.Tag == PtxTag.Argb8888PaddingBe
                        ? PtxFormats.Parse("ARGB8888_Padding_BE")
                        : PtxFormats.Parse("ARGB8888_BE");
                }

                if (fmt0Mode == "ABGR" || (hinted.Valid && hinted.Tag == PtxTag.Abgr8888))
                {
                    return PtxFormats.Parse("ABGR8888");
                }

                return PtxFormats.Parse("ARGB8888");
            case 1:
                return PtxFormats.Parse("RGBA4444");
            case 2:
                return PtxFormats.Parse("RGB565");
            case 3:
                return PtxFormats.Parse("RGBA5551");
            case 5:
                return bigEndianHeader ? PtxFormats.Parse("DXT5_RGBA_BE") : PtxFormats.Parse("DXT5_RGBA_MortonBlock");
            case 21:
                return PtxFormats.Parse("RGBA4444_Block");
            case 22:
                return PtxFormats.Parse("RGB565_Block");
            case 23:
                return PtxFormats.Parse("RGBA5551_Block");
            case 30:
                return PtxFormats.Parse("PVRTC_4BPP_RGBA");
            case 31:
                return PtxFormats.Parse("PVRTC_2BPP_RGBA");
            case 32:
                return PtxFormats.Parse("ETC1_RGB");
            case 35:
                return PtxFormats.Parse("DXT1_RGB");
            case 36:
                return PtxFormats.Parse("DXT3_RGBA");
            case 37:
                return PtxFormats.Parse("DXT5_RGBA");
            case 160:
                return PtxFormats.Parse("ASTC_4x4");
            case 161:
                return PtxFormats.Parse("ASTC_5x5");
            case 162:
                return PtxFormats.Parse("ASTC_6x6");
            case 163:
                return PtxFormats.Parse("ASTC_8x8");
            case 147:
            case 148:
            case 149:
            case 150:
            {
                var tag = header.Format is 147 or 150
                    ? PtxTag.Etc1Rgb
                    : header.Format == 148
                        ? PtxTag.Pvrtc4BppRgba
                        : fmt0Mode == "ABGR" || (hinted.Valid && hinted.Tag == PtxTag.Xbgr8888AuxAlpha)
                            ? PtxTag.Xbgr8888AuxAlpha
                            : PtxTag.Xrgb8888AuxAlpha;

                if (hinted.Valid && hinted.FileId == header.Format && hinted.Alpha != PtxAlphaScheme.None)
                {
                    return new PtxFormatSpec(tag, header.Format, hinted.Alpha, false, hinted.MarkAlpha64, true);
                }

                var pixelCount = (int)header.Width * (int)header.Height;
                var tex0Size = CalcAuxColorPayloadSize(header.Format, (int)header.Width, (int)header.Height);
                if (tex0Size > payload.Length)
                {
                    throw new InvalidDataException("PTX payload truncated");
                }

                var detection = PtxAlpha.AutoDetect(header.Format, payload[tex0Size..], pixelCount, header.AlphaSize, header.AlphaFormat);
                var alpha = detection.Valid ? detection.Scheme : PtxAlphaScheme.None;
                return new PtxFormatSpec(tag, header.Format, alpha, false, header.AlphaFormat == 0x64, true);
            }
            default:
                throw new InvalidDataException("Unsupported PTX format id");
        }
    }

    private static int CalcAuxColorPayloadSize(uint fileId, int width, int height)
    {
        return fileId switch
        {
            147 or 150 => (((width + 3) & ~3) * ((height + 3) & ~3)) >> 1,
            148 => (NextPowerOfTwoMin8(width) * NextPowerOfTwoMin8(height)) >> 1,
            149 => width * height * 4,
            _ => 0,
        };
    }

    private static int NextPowerOfTwoMin8(int value)
    {
        var result = 8;
        while (result < value)
        {
            result <<= 1;
        }

        return result;
    }

    private static PtxTag ColorOnlyTagForAux(PtxFormatSpec spec) => spec.FileId switch
    {
        147 or 150 => PtxTag.Etc1Rgb,
        148 => PtxTag.Pvrtc4BppRgba,
        _ => spec.Tag,
    };

    private static bool ShouldWriteAlpha64(PtxFormatSpec spec) => spec.MarkAlpha64 || PtxFormats.IsAstcFileId(spec.FileId);

    private static bool IsVariableAlphaSize(PtxAlphaScheme scheme) =>
        scheme is PtxAlphaScheme.A8_A1 or PtxAlphaScheme.Palette4_A1 or PtxAlphaScheme.Palette5_A1;

    private static void DecodeSimple(PtxTag tag, ReadOnlySpan<byte> payload, int width, int height, string fmt0Mode, Span<byte> pixels)
    {
        var stream = new BufferReader(payload);
        switch (tag)
        {
            case PtxTag.Argb8888:
                if (fmt0Mode == "ABGR")
                {
                    PixelFormats.ReadAbgr8888(stream, pixels, width * height);
                }
                else if (fmt0Mode == "ARGB_Padding")
                {
                    var padWidth = width;
                    if ((padWidth % 64) != 0)
                    {
                        padWidth = (padWidth / 64) * 64 + 64;
                    }

                    BlockFormats.ReadArgb8888Padding(stream, width, height, padWidth << 2, pixels);
                }
                else
                {
                    PixelFormats.ReadArgb8888(stream, pixels, width * height);
                }

                break;
            case PtxTag.Abgr8888:
                PixelFormats.ReadAbgr8888(stream, pixels, width * height);
                break;
            case PtxTag.Rgba4444:
                PixelFormats.ReadRgba4444(stream, pixels, width * height);
                break;
            case PtxTag.Rgb565:
                PixelFormats.ReadRgb565(stream, pixels, width * height);
                break;
            case PtxTag.Rgba5551:
                PixelFormats.ReadRgba5551(stream, pixels, width * height);
                break;
            case PtxTag.Rgba4444Block:
                BlockFormats.ReadRgba4444Block(stream, width, height, pixels);
                break;
            case PtxTag.Rgb565Block:
                BlockFormats.ReadRgb565Block(stream, width, height, pixels);
                break;
            case PtxTag.Rgba5551Block:
                BlockFormats.ReadRgba5551Block(stream, width, height, pixels);
                break;
            case PtxTag.Argb8888Be:
                stream.BigEndian = true;
                PixelFormats.ReadArgb8888(stream, pixels, width * height);
                break;
            case PtxTag.Argb8888PaddingBe:
                stream.BigEndian = true;
                var padWidthBe = width;
                if ((padWidthBe % 64) != 0)
                {
                    padWidthBe = (padWidthBe / 64) * 64 + 64;
                }

                BlockFormats.ReadArgb8888Padding(stream, width, height, padWidthBe << 2, pixels);
                break;
            case PtxTag.Dxt1Rgb:
                Dxt.DecodeDxt1(stream, width, height, pixels);
                break;
            case PtxTag.Dxt3Rgba:
                Dxt.DecodeDxt3(stream, width, height, pixels);
                break;
            case PtxTag.Dxt5Rgba:
                Dxt.DecodeDxt5(stream, width, height, pixels);
                break;
            case PtxTag.Dxt5RgbaMortonBlock:
                DxtMorton.DecodeDxt5MortonBlock(stream, width, height, pixels);
                break;
            case PtxTag.Dxt5RgbaBe:
                stream.BigEndian = false;
                Dxt.DecodeDxt5(stream, width, height, pixels);
                break;
            case PtxTag.Etc1Rgb:
                Etc1Codec.Decode(payload, width, height, pixels);
                break;
            case PtxTag.Pvrtc4BppRgba:
                PvrtcCodec.DecodePaddedRgba(payload, width, height, pixels);
                break;
            case PtxTag.Pvrtc2BppRgba:
                throw new InvalidDataException("PVRTC 2BPP is not supported");
            case PtxTag.Astc4X4:
                AstcCodec.Decode(payload, width, height, 4, 4, pixels);
                break;
            case PtxTag.Astc5X5:
                AstcCodec.Decode(payload, width, height, 5, 5, pixels);
                break;
            case PtxTag.Astc6X6:
                AstcCodec.Decode(payload, width, height, 6, 6, pixels);
                break;
            case PtxTag.Astc8X8:
                AstcCodec.Decode(payload, width, height, 8, 8, pixels);
                break;
            case PtxTag.Xrgb8888AuxAlpha:
                PixelFormats.ReadXrgb8888(stream, pixels, width * height);
                break;
            case PtxTag.Xbgr8888AuxAlpha:
                for (var i = 0; i < width * height; i++)
                {
                    var t = stream.ReadUInt32();
                    var o = i * 4;
                    pixels[o] = (byte)(t & 0xFF);
                    pixels[o + 1] = (byte)((t >> 8) & 0xFF);
                    pixels[o + 2] = (byte)((t >> 16) & 0xFF);
                    pixels[o + 3] = 255;
                }

                break;
            default:
                throw new InvalidDataException("Unsupported PTX decode tag");
        }
    }

    private static uint EncodeSimple(PtxTag tag, BufferWriter writer, ImageBitmap bitmap, string fmt0Mode)
    {
        var pixels = bitmap.Pixels;
        var width = bitmap.Width;
        var height = bitmap.Height;
        var count = bitmap.Size;
        switch (tag)
        {
            case PtxTag.Argb8888:
                if (fmt0Mode == "ABGR")
                {
                    PixelFormats.WriteAbgr8888(writer, pixels, count);
                }
                else if (fmt0Mode == "ARGB_Padding")
                {
                    var padWidth = width;
                    if ((padWidth % 64) != 0)
                    {
                        padWidth = (padWidth / 64) * 64 + 64;
                    }

                    BlockFormats.WriteArgb8888Padding(writer, width, height, padWidth << 2, pixels);
                }
                else
                {
                    PixelFormats.WriteArgb8888(writer, pixels, count);
                }

                return (uint)(width << 2);
            case PtxTag.Abgr8888:
                PixelFormats.WriteAbgr8888(writer, pixels, count);
                return (uint)(width << 2);
            case PtxTag.Rgba4444:
                PixelFormats.WriteRgba4444(writer, pixels, count);
                return (uint)(width << 1);
            case PtxTag.Rgb565:
                PixelFormats.WriteRgb565(writer, pixels, count);
                return (uint)(width << 1);
            case PtxTag.Rgba5551:
                PixelFormats.WriteRgba5551(writer, pixels, count);
                return (uint)(width << 1);
            case PtxTag.Rgba4444Block:
                return BlockFormats.WriteRgba4444Block(writer, width, height, pixels);
            case PtxTag.Rgb565Block:
                return BlockFormats.WriteRgb565Block(writer, width, height, pixels);
            case PtxTag.Rgba5551Block:
                return BlockFormats.WriteRgba5551Block(writer, width, height, pixels);
            case PtxTag.Xrgb8888AuxAlpha:
                PixelFormats.WriteXrgb8888(writer, pixels, count);
                return (uint)(width << 2);
            case PtxTag.Xbgr8888AuxAlpha:
                for (var i = 0; i < count; i++)
                {
                    var o = i * 4;
                    writer.WriteUInt32(0xFF000000u | (uint)(pixels[o + 2] << 16) | (uint)(pixels[o + 1] << 8) | pixels[o]);
                }

                return (uint)(width << 2);
            case PtxTag.Argb8888Be:
                writer.BigEndian = true;
                PixelFormats.WriteArgb8888(writer, pixels, count);
                return (uint)(width << 2);
            case PtxTag.Argb8888PaddingBe:
                writer.BigEndian = true;
                var padWidthBe = width;
                if ((padWidthBe % 64) != 0)
                {
                    padWidthBe = (padWidthBe / 64) * 64 + 64;
                }

                return BlockFormats.WriteArgb8888Padding(writer, width, height, padWidthBe << 2, pixels);
            case PtxTag.Dxt1Rgb:
                return Dxt.EncodeDxt1(writer, width, height, pixels);
            case PtxTag.Dxt3Rgba:
                return Dxt.EncodeDxt3(writer, width, height, pixels);
            case PtxTag.Dxt5Rgba:
                return Dxt.EncodeDxt5(writer, width, height, pixels);
            case PtxTag.Dxt5RgbaMortonBlock:
                return DxtMorton.EncodeDxt5MortonBlock(writer, width, height, pixels);
            case PtxTag.Dxt5RgbaBe:
                Dxt.EncodeDxt5(writer, width, height, pixels);
                return (uint)width;
            case PtxTag.Etc1Rgb:
                writer.WriteBytes(Etc1Codec.Encode(pixels, width, height));
                return (uint)(width >> 1);
            case PtxTag.Pvrtc4BppRgba:
                writer.WriteBytes(PvrtcCodec.EncodePaddedRgba(pixels, width, height));
                return (uint)(NextPowerOfTwoMin8(Math.Max(8, width)) >> 1);
            case PtxTag.Pvrtc2BppRgba:
                throw new InvalidDataException("PVRTC 2BPP is not supported");
            case PtxTag.Astc4X4:
                writer.WriteBytes(AstcCodec.Encode(bitmap.PixelMemory, width, height, 4, 4));
                return (uint)width;
            case PtxTag.Astc5X5:
                writer.WriteBytes(AstcCodec.Encode(bitmap.PixelMemory, width, height, 5, 5));
                return (uint)(width * 16 / 25);
            case PtxTag.Astc6X6:
                writer.WriteBytes(AstcCodec.Encode(bitmap.PixelMemory, width, height, 6, 6));
                return (uint)(width * 4 / 9);
            case PtxTag.Astc8X8:
                writer.WriteBytes(AstcCodec.Encode(bitmap.PixelMemory, width, height, 8, 8));
                return (uint)(width / 4);
            default:
                throw new InvalidDataException("Unsupported PTX encode tag");
        }
    }
}

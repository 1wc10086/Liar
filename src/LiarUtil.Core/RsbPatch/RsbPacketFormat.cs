using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.RsbPatch;

internal sealed class RsbPacketHeader
{
    public uint Unknown1;
    public uint Unknown2;
    public uint ResourceDataSectionCompression;
    public uint InformationSectionSize;
    public uint GeneralResourceDataSectionOffset;
    public uint GeneralResourceDataSectionSize;
    public uint GeneralResourceDataSectionSizeOriginal;
    public uint TextureResourceDataSectionOffset;
    public uint TextureResourceDataSectionSize;
    public uint TextureResourceDataSectionSizeOriginal;
    public uint ResourceInformationSectionSize;
    public uint ResourceInformationSectionOffset;

    public bool CompressGeneral => (ResourceDataSectionCompression & RsbPatchFormat.CompressionGeneral) != 0;

    public bool CompressTexture => (ResourceDataSectionCompression & RsbPatchFormat.CompressionTexture) != 0;

    public int EndOffset => (int)Math.Max(
        InformationSectionSize,
        Math.Max(
            GeneralResourceDataSectionOffset + GeneralResourceDataSectionSize,
            TextureResourceDataSectionOffset + TextureResourceDataSectionSize));
}

internal static class RsbPacketFormat
{
    private const int HeaderPrefixSize = 8;

    private const int HeaderWordCount = 21;

    private const int HeaderSize = HeaderPrefixSize + HeaderWordCount * sizeof(uint);

    public static RsbPacketHeader ReadHeader(byte[] data)
    {
        if (data.Length < HeaderSize)
        {
            throw new RsbPatchException(LiarUtil.Core.Strings.RSGPPacketHeaderIncomplete);
        }

        if (new BufferReader(data).ReadUInt32() != RsbPatchFormat.PacketMagic)
        {
            throw new RsbPatchException(LiarUtil.Core.Strings.NotValidRSGPPacket);
        }

        var offset = HeaderPrefixSize;
        var header = new RsbPacketHeader
        {
            Unknown1 = Read(data, ref offset),
            Unknown2 = Read(data, ref offset),
            ResourceDataSectionCompression = Read(data, ref offset),
            InformationSectionSize = Read(data, ref offset),
            GeneralResourceDataSectionOffset = Read(data, ref offset),
            GeneralResourceDataSectionSize = Read(data, ref offset),
            GeneralResourceDataSectionSizeOriginal = Read(data, ref offset),
        };
        offset += sizeof(uint);
        header.TextureResourceDataSectionOffset = Read(data, ref offset);
        header.TextureResourceDataSectionSize = Read(data, ref offset);
        header.TextureResourceDataSectionSizeOriginal = Read(data, ref offset);
        offset += 5 * sizeof(uint);
        header.ResourceInformationSectionSize = Read(data, ref offset);
        header.ResourceInformationSectionOffset = Read(data, ref offset);
        return header;
    }

    public static byte[] Uncompress(byte[] packet)
    {
        var header = ReadHeader(packet);
        var general = ReadStored(packet, header.GeneralResourceDataSectionOffset, header.GeneralResourceDataSectionSize, header.GeneralResourceDataSectionSizeOriginal, header.CompressGeneral);
        var texture = ReadStored(
            packet,
            header.TextureResourceDataSectionOffset,
            header.TextureResourceDataSectionSize,
            header.TextureResourceDataSectionSizeOriginal,
            header.CompressTexture && header.TextureResourceDataSectionSizeOriginal != 0);

        if (header.InformationSectionSize > packet.Length)
        {
            throw new RsbPatchException(LiarUtil.Core.Strings.RSGPPacketInfoRegionOutOfRange);
        }

        var writer = new RsbPatchWriter((int)header.InformationSectionSize + general.Length + texture.Length + 3 * RsbPatchFormat.PaddingUnit);
        writer.WriteBytes(packet.AsSpan(0, (int)header.InformationSectionSize));
        writer.WritePadding();
        writer.WriteBytes(general);
        writer.WritePadding();
        writer.WriteBytes(texture);
        writer.WritePadding();
        return writer.ToArray();
    }

    public static byte[] Compress(byte[] packet)
    {
        var header = ReadHeader(packet);
        var generalOffset = RsbPatchFormat.Align((int)header.InformationSectionSize);
        var textureOffset = RsbPatchFormat.Align(generalOffset + (int)header.GeneralResourceDataSectionSizeOriginal);
        if (generalOffset > packet.Length || (int)header.GeneralResourceDataSectionSizeOriginal > packet.Length - generalOffset ||
            textureOffset > packet.Length || (int)header.TextureResourceDataSectionSizeOriginal > packet.Length - textureOffset)
        {
            throw new RsbPatchException(LiarUtil.Core.Strings.RSGPRawPacketSectionOutOfRange);
        }

        var writer = new RsbPatchWriter((int)header.InformationSectionSize + (int)header.GeneralResourceDataSectionSizeOriginal + (int)header.TextureResourceDataSectionSizeOriginal + 2 * RsbPatchFormat.PaddingUnit);
        writer.WriteBytes(packet.AsSpan(0, (int)header.InformationSectionSize));
        writer.WritePadding();

        header.GeneralResourceDataSectionOffset = (uint)writer.Length;
        var general = packet.AsSpan(generalOffset, (int)header.GeneralResourceDataSectionSizeOriginal);
        if (header.CompressGeneral)
        {
            writer.WriteBytes(RsbPatchCompression.Compress(general));
            writer.WritePadding();
        }
        else
        {
            writer.WriteBytes(general);
        }

        header.GeneralResourceDataSectionSize = (uint)writer.Length - header.GeneralResourceDataSectionOffset;

        header.TextureResourceDataSectionOffset = (uint)writer.Length;
        var texture = packet.AsSpan(textureOffset, (int)header.TextureResourceDataSectionSizeOriginal);
        if (header.CompressTexture && header.TextureResourceDataSectionSizeOriginal != 0)
        {
            writer.WriteBytes(RsbPatchCompression.Compress(texture));
            writer.WritePadding();
        }
        else
        {
            writer.WriteBytes(texture);
        }

        header.TextureResourceDataSectionSize = (uint)writer.Length - header.TextureResourceDataSectionOffset;

        var result = writer.ToArray();
        WriteHeader(result, header);
        return result;
    }

    private static byte[] ReadStored(byte[] packet, uint offset, uint size, uint originalSize, bool compressed)
    {
        if (offset > packet.Length || size > packet.Length - offset)
        {
            throw new RsbPatchException(LiarUtil.Core.Strings.RSGPPacketSectionOutOfRange);
        }

        if (!compressed)
        {
            return packet.AsSpan((int)offset, (int)size).ToArray();
        }

        return RsbPatchCompression.Decompress(packet.AsSpan((int)offset, (int)size), (int)originalSize);
    }

    private static void WriteHeader(byte[] packet, RsbPacketHeader header)
    {
        var offset = HeaderPrefixSize;
        Write(packet, ref offset, header.Unknown1);
        Write(packet, ref offset, header.Unknown2);
        Write(packet, ref offset, header.ResourceDataSectionCompression);
        Write(packet, ref offset, header.InformationSectionSize);
        Write(packet, ref offset, header.GeneralResourceDataSectionOffset);
        Write(packet, ref offset, header.GeneralResourceDataSectionSize);
        Write(packet, ref offset, header.GeneralResourceDataSectionSizeOriginal);
        Write(packet, ref offset, 0);
        Write(packet, ref offset, header.TextureResourceDataSectionOffset);
        Write(packet, ref offset, header.TextureResourceDataSectionSize);
        Write(packet, ref offset, header.TextureResourceDataSectionSizeOriginal);
        for (var index = 0; index < 5; index++)
        {
            Write(packet, ref offset, 0);
        }

        Write(packet, ref offset, header.ResourceInformationSectionSize);
        Write(packet, ref offset, header.ResourceInformationSectionOffset);
        for (var index = 0; index < 3; index++)
        {
            Write(packet, ref offset, 0);
        }
    }

    private static uint Read(byte[] data, ref int offset)
    {
        var value = new BufferReader(data, offset, sizeof(uint)).ReadUInt32();
        offset += sizeof(uint);
        return value;
    }

    private static void Write(byte[] data, ref int offset, uint value)
    {
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset, sizeof(uint)), value);
        offset += sizeof(uint);
    }
}

using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Dz.Buffers;

internal sealed class DzStreamReader(Stream stream)
{
    public byte ReadByte()
    {
        var value = stream.ReadByte();
        return value < 0 ? throw new DzException(LiarUtil.Core.Strings.DataLengthInsufficient) : (byte)value;
    }

    public ushort ReadUInt16() => new BufferReader(ReadBytes(sizeof(ushort))).ReadUInt16();

    public uint ReadUInt32() => new BufferReader(ReadBytes(sizeof(uint))).ReadUInt32();

    public string ReadName() => TextCodec.DecodeLenient(ReadNullTerminatedBytes(), TextFormat.Latin1);

    public byte[] ReadBytes(int count)
    {
        if (count < 0)
        {
            throw new DzException(string.Format(LiarUtil.Core.Strings.InvalidDataLength0, count));
        }

        var buffer = new byte[count];
        var offset = 0;
        while (offset < count)
        {
            var read = stream.Read(buffer, offset, count - offset);
            if (read <= 0)
            {
                throw new DzException(LiarUtil.Core.Strings.DataLengthInsufficient);
            }
            offset += read;
        }

        return buffer;
    }

    private byte[] ReadNullTerminatedBytes()
    {
        using var buffer = new MemoryStream();
        while (true)
        {
            var value = stream.ReadByte();
            if (value < 0)
            {
                throw new DzException(LiarUtil.Core.Strings.StringLacksTerminator);
            }
            if (value == 0)
            {
                return buffer.ToArray();
            }
            buffer.WriteByte((byte)value);
        }
    }
}

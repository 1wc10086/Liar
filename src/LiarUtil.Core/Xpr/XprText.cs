using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Xpr;

internal static class XprText
{
    public static string? TypeToString(uint type)
    {
        if (type == 0)
        {
            return null;
        }

        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        bytes[0] = (byte)(type >> 24);
        bytes[1] = (byte)(type >> 16);
        bytes[2] = (byte)(type >> 8);
        bytes[3] = (byte)type;
        return TextCodec.Decode(bytes, TextFormat.Latin1);
    }

    public static uint StringToType(string? type)
    {
        if (string.IsNullOrEmpty(type))
        {
            return 0;
        }

        var encoded = TextCodec.Encode(type, TextFormat.Latin1);
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        bytes.Clear();
        encoded.AsSpan(0, Math.Min(bytes.Length, encoded.Length)).CopyTo(bytes);
        return new BufferReader(bytes, ByteOrder.Big).ReadUInt32();
    }

    public static string ReadName(ReadOnlySpan<byte> pool, int offset)
    {
        if (offset < 0 || offset >= pool.Length)
        {
            throw new XprException(LiarUtil.Core.Strings.XPRNamePoolOffsetOutOfRange);
        }

        var window = pool[offset..];
        var terminator = window.IndexOf((byte)0);
        return terminator < 0
            ? throw new XprException(LiarUtil.Core.Strings.XPRNameNotZeroTerminated)
            : TextCodec.Decode(window[..terminator], TextFormat.Latin1);
    }

    public static string ToNativePath(string recordPath) => recordPath.Replace('\\', '/');
}

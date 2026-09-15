using XMemCompressionDotNet;

namespace LiarUtil.Core.Pak.Packing;

internal static class XmemPack
{
    public static byte[] Compress(byte[] payload) => payload.Length == 0
        ? payload
        : XMem.XMemCompressLzxTdBuffer(payload);

    public static byte[] Decompress(byte[] payload) => payload.Length == 0
        ? payload
        : XMem.XMemDecompressLzxTdBuffer(payload);
}

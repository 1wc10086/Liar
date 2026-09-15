namespace LiarUtil.Core.Core.Binary;

public static class ZigZag
{
    public static uint Encode32(int value) => (uint)((value << 1) ^ (value >> 31));

    public static int Decode32(uint value) => (int)((value >> 1) ^ (uint)-(int)(value & 1));

    public static ulong Encode64(long value) => ((ulong)value << 1) ^ (ulong)(value >> 63);

    public static long Decode64(ulong value) => (long)((value >> 1) ^ (ulong)-(long)(value & 1));
}

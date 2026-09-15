using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Arcv;

internal static class ArcvExtensions
{
    public const string Fallback = ".dat";

    public static string DetectExtension(ReadOnlySpan<byte> data) => data.Length < sizeof(uint)
        ? Fallback
        : new BufferReader(data, ByteOrder.Big).ReadUInt32() switch
        {
            0x5247434E => ".NCGR",
            0x5243534E => ".NSCR",
            0x524C434E => ".NCLR",
            0x524E414E => ".NANR",
            0x5245434E => ".NCER",
            0x52414D4E => ".NMAR",
            0x52434D4E => ".NMCR",
            0x5254464E => ".NFTR",
            0x53444154 => ".sdat",
            0x4E415243 => ".narc",
            _ => Fallback,
        };
}

using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Pak.Models;

namespace LiarUtil.Core.Pak.Packing;

internal static class PakPayloadDecoder
{
    public static PakPayload Decode(byte[] data)
    {
        if (data.Length < sizeof(int))
        {
            throw new PakException(LiarUtil.Core.Strings.PAKFileEmptyOrTruncated);
        }

        return new BufferReader(data).ReadInt32() switch
        {
            PakFormat.PcMagic => new PakPayload(PakXor.Transform(data), true, false, false),
            PakFormat.NormalMagic => new PakPayload(data, false, false, false),
            PakFormat.XmemMagic => new PakPayload(XmemPack.Decompress(data), false, false, true),
            PakFormat.TvZipMagic => new PakPayload(data, false, true, false),
            var magic => throw new PakException(string.Format(LiarUtil.Core.Strings.UnknownPAKFileType0x0X8, magic)),
        };
    }
}

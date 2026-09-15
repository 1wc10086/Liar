using LiarUtil.Core.RsbPatch;

namespace LiarUtil.Core.Services;

public sealed class RsbPatchService
{
    public void Encode(string beforePath, string afterPath, string patchPath, bool useRawPacket) =>
        RsbPatchCodec.Encode(beforePath, afterPath, patchPath, useRawPacket);

    public void Decode(string patchPath, string beforePath, string afterPath, bool useRawPacket) =>
        RsbPatchCodec.Decode(beforePath, patchPath, afterPath, useRawPacket);
}

using LiarUtil.Core.Core.IO;

namespace LiarUtil.Core.RsbPatch;

public static class RsbPatchCodec
{
    public static void Encode(string beforePath, string afterPath, string patchPath, bool useRawPacket = false)
    {
        var output = RsbPatchProcessor.Encode(new RsbPatchEncodeOptions
        {
            Before = File.ReadAllBytes(beforePath),
            After = File.ReadAllBytes(afterPath),
            UseRawPacket = useRawPacket,
        });
        FileIO.WriteAllBytes(patchPath, output);
    }

    public static void Decode(string beforePath, string patchPath, string afterPath, bool useRawPacket = false)
    {
        var output = RsbPatchProcessor.Decode(new RsbPatchDecodeOptions
        {
            Before = File.ReadAllBytes(beforePath),
            Patch = File.ReadAllBytes(patchPath),
            UseRawPacket = useRawPacket,
        });
        FileIO.WriteAllBytes(afterPath, output);
    }
}

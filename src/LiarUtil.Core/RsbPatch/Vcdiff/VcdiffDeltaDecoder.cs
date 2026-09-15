using VCDiff.Decoders;
using VCDiff.Includes;

namespace LiarUtil.Core.RsbPatch;

internal static class VcdiffDeltaDecoder
{
    private const int MaximumTargetSize = 0x7FFFFFFF;

    public static byte[] Decode(byte[] source, byte[] delta)
    {
        if (delta.Length == 0)
        {
            return source.ToArray();
        }

        using var sourceStream = new MemoryStream(source, writable: false);
        using var deltaStream = new MemoryStream(delta, writable: false);
        using var outputStream = new MemoryStream();
        using var decoder = new VcDecoder(sourceStream, deltaStream, outputStream, MaximumTargetSize);
        var result = decoder.Decode(out _);
        if (result != VCDiffResult.SUCCESS)
        {
            throw new RsbPatchException(string.Format(LiarUtil.Core.Strings.VCDiffDecodingFailed0, result));
        }

        return outputStream.ToArray();
    }
}

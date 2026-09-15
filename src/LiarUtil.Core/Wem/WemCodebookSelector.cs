using NVorbis;

namespace LiarUtil.Core.Wem;

internal static class WemCodebookSelector
{
    public static byte[] Select(byte[] wem, WemForcePacketFormat forcePacketFormat)
    {
        var candidates = new[] { PackedCodebooks.LoadAoVu(), PackedCodebooks.LoadDefault() };
        foreach (var candidate in candidates)
        {
            try
            {
                var ogg = WemToOgg.Convert(wem, candidate, forcePacketFormat);
                if (IsDecodable(ogg))
                {
                    return candidate;
                }
            }
            catch (Exception)
            {
            }
        }

        return candidates[^1];
    }

    private static bool IsDecodable(byte[] ogg)
    {
        try
        {
            using var stream = new MemoryStream(ogg, writable: false);
            using var reader = new VorbisReader(stream);
            return reader.Channels > 0 && reader.SampleRate > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

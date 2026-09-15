namespace LiarUtil.Core.Core.Compression;

internal static class StreamCodec
{
    public static byte[] Read(Stream source, int expectedSize = 0)
    {
        using (source)
        {
            using var output = expectedSize > 0 ? new MemoryStream(expectedSize) : new MemoryStream();
            source.CopyTo(output);
            return output.ToArray();
        }
    }
}

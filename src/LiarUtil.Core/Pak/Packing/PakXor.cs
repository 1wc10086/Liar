namespace LiarUtil.Core.Pak.Packing;

internal static class PakXor
{
    public static byte[] Transform(byte[] data)
    {
        var result = (byte[])data.Clone();
        for (var i = 0; i < result.Length; i++)
        {
            result[i] ^= PakFormat.XorKey;
        }

        return result;
    }

    public static void Transform(string sourcePath, string outputPath)
    {
        using var input = File.OpenRead(sourcePath);
        using var output = File.Create(outputPath);
        var buffer = new byte[81920];
        int read;
        while ((read = input.Read(buffer)) > 0)
        {
            for (var i = 0; i < read; i++)
            {
                buffer[i] ^= PakFormat.XorKey;
            }

            output.Write(buffer, 0, read);
        }
    }
}

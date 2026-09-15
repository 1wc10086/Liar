namespace LiarUtil.Core.Core.Crypto;

public static class CipherExtensions
{
    extension(Cipher cipher)
    {
        public byte[] Encrypt(ReadOnlySpan<byte> input, BlockCipherMode mode = BlockCipherMode.Ecb)
        {
            var output = new byte[input.Length];
            cipher.Encrypt(input, output, mode);
            return output;
        }

        public byte[] Decrypt(ReadOnlySpan<byte> input, BlockCipherMode mode = BlockCipherMode.Ecb)
        {
            var output = new byte[input.Length];
            cipher.Decrypt(input, output, mode);
            return output;
        }
    }

    public static bool IsBlockAligned(ReadOnlySpan<byte> data, int blockSize) =>
        !data.IsEmpty && data.Length % blockSize == 0;

    public static byte[] PadZero(ReadOnlySpan<byte> data, int blockSize)
    {
        var remainder = data.Length % blockSize;
        if (remainder == 0)
        {
            return data.ToArray();
        }

        var result = new byte[data.Length + (blockSize - remainder)];
        data.CopyTo(result);
        return result;
    }

    public static byte[] UnpadZero(ReadOnlySpan<byte> data)
    {
        var end = data.Length;
        while (end > 0 && data[end - 1] == 0)
        {
            end--;
        }

        return data[..end].ToArray();
    }
}

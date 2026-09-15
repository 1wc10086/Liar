using System.Security.Cryptography;
using LiarUtil.Core.Core.Crypto;
using PopCapCipher = LiarUtil.Core.Core.Crypto.Cipher;

namespace LiarUtil.Core.Rton;

public static class RtonCrypto
{
    private const int BlockBytes = 24;

    public static (byte[] Key, byte[] Iv) GenerateKeyAndIv(string password) =>
        CipherFactory.DeriveKeyAndIv(password, HashAlgorithmName.MD5, BlockBytes, BlockBytes, 4);

    public static byte[] DecryptBytes(ReadOnlySpan<byte> bytes, string password)
    {
        if (bytes.Length < 2)
        {
            return [];
        }

        var payload = bytes[2..].ToArray();
        if (payload.Length % BlockBytes != 0)
        {
            return [];
        }

        var (key, iv) = GenerateKeyAndIv(password);
        try
        {
            return CipherExtensions.UnpadZero(PopCapCipher.Make(key, iv, BlockBytes).Decrypt(payload, BlockCipherMode.Cbc));
        }
        catch
        {
            return payload;
        }
    }

    public static byte[] EncryptBytes(ReadOnlySpan<byte> bytes, string password)
    {
        var (key, iv) = GenerateKeyAndIv(password);
        var padded = CipherExtensions.PadZero(bytes, BlockBytes);
        var encrypted = PopCapCipher.Make(key, iv, BlockBytes).Encrypt(padded, BlockCipherMode.Cbc);

        var result = new byte[encrypted.Length + 2];
        result[0] = 0x10;
        encrypted.CopyTo(result, 2);
        return result;
    }
}

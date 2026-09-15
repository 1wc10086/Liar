using System.Security.Cryptography;
using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Core.Crypto;

public static class CipherFactory
{
    public static (byte[] Key, byte[] Iv) DeriveKeyAndIv(
        string password,
        HashAlgorithmName algorithm,
        int keyLength,
        int ivLength,
        int ivOffset = 0)
    {
        var hash = Convert.ToHexStringLower(HashData(algorithm, TextCodec.Encode(password)));
        var material = TextCodec.Encode(hash, TextFormat.Ascii);
        if (material.Length < ivOffset + ivLength)
        {
            throw new CryptographicException(LiarUtil.Core.Strings.DerivedKeyMaterialInsufficient);
        }

        var key = Derive(material, keyLength);
        var iv = material.AsSpan(ivOffset, ivLength).ToArray();
        return (key, iv);
    }

    public static byte[] HashData(HashAlgorithmName algorithm, ReadOnlySpan<byte> data) => algorithm.Name switch
    {
        "MD5" => MD5.HashData(data),
        "SHA1" => SHA1.HashData(data),
        "SHA256" => SHA256.HashData(data),
        "SHA384" => SHA384.HashData(data),
        "SHA512" => SHA512.HashData(data),
        _ => throw new CryptographicException(string.Format(LiarUtil.Core.Strings.UnsupportedHashAlgorithm0, algorithm.Name)),
    };

    public static (byte[] Key, byte[] Iv) DerivePaddedKeyAndIv(
        string password,
        HashAlgorithmName algorithm,
        int keyLength,
        int ivLength,
        byte padding = (byte)'0')
    {
        var hash = Convert.ToHexStringLower(HashData(algorithm, TextCodec.Encode(password)));
        var material = TextCodec.Encode(hash, TextFormat.Ascii);
        return (Derive(material, keyLength, padding), Derive(material, ivLength, padding));
    }

    private static byte[] Derive(ReadOnlySpan<byte> material, int length, byte padding = 0)
    {
        if (material.Length >= length)
        {
            return material[..length].ToArray();
        }

        var result = new byte[length];
        material.CopyTo(result);
        result.AsSpan(material.Length).Fill(padding);
        return result;
    }
}

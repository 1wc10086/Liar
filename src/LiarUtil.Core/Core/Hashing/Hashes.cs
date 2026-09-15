using System.Security.Cryptography;

namespace LiarUtil.Core.Core.Hashing;

public static class Hashes
{
    public static byte[] Compute(HashAlgorithmKind kind, ReadOnlySpan<byte> data) => kind switch
    {
        HashAlgorithmKind.Md5 => MD5.HashData(data),
        HashAlgorithmKind.Sha1 => SHA1.HashData(data),
        HashAlgorithmKind.Sha256 => SHA256.HashData(data),
        HashAlgorithmKind.Sha384 => SHA384.HashData(data),
        _ => SHA512.HashData(data),
    };

    public static byte[] Md5(ReadOnlySpan<byte> data) => MD5.HashData(data);

    public static string Md5HexLower(ReadOnlySpan<byte> data) => Convert.ToHexStringLower(MD5.HashData(data));

    public static string Md5HexUpper(ReadOnlySpan<byte> data) => Convert.ToHexString(MD5.HashData(data));

    public static bool Verify(HashAlgorithmKind kind, ReadOnlySpan<byte> data, ReadOnlySpan<byte> expected) =>
        CryptographicOperations.FixedTimeEquals(Compute(kind, data), expected);

    public static bool VerifyMd5(ReadOnlySpan<byte> data, ReadOnlySpan<byte> expected) =>
        CryptographicOperations.FixedTimeEquals(MD5.HashData(data), expected);
}

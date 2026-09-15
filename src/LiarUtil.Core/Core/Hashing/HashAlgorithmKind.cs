namespace LiarUtil.Core.Core.Hashing;

public enum HashAlgorithmKind
{
    Md5 = 0,
    Sha1 = 1,
    Sha256 = 2,
    Sha384 = 3,
    Sha512 = 4,
}

public static class HashAlgorithmKindExtensions
{
    extension(HashAlgorithmKind kind)
    {
        public int Size => kind switch
        {
            HashAlgorithmKind.Md5 => 16,
            HashAlgorithmKind.Sha1 => 20,
            HashAlgorithmKind.Sha256 => 32,
            HashAlgorithmKind.Sha384 => 48,
            _ => 64,
        };

        public string DisplayName => kind switch
        {
            HashAlgorithmKind.Md5 => "MD5",
            HashAlgorithmKind.Sha1 => "SHA-1",
            HashAlgorithmKind.Sha256 => "SHA-256",
            HashAlgorithmKind.Sha384 => "SHA-384",
            _ => "SHA-512",
        };
    }
}

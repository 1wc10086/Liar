namespace LiarUtil.Core.Rton;

public enum StringEncoding
{
    Utf8,
    Eascii,
}

public enum RtonFunction
{
    Decode,
    Encode,
    Decrypt,
    Encrypt,
}

public static class RtonConstants
{
    public const string DefaultKey = "com_popcap_pvz2_magento_product_2013_05_05";

    internal static ReadOnlySpan<byte> RtonHeader => [82, 84, 79, 78, 1, 0, 0, 0];

    internal static ReadOnlySpan<byte> DoneFooter => [68, 79, 78, 69];
}

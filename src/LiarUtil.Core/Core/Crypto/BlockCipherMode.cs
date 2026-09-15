namespace LiarUtil.Core.Core.Crypto;

public enum BlockCipherMode : byte
{
    Ecb = 0,
    Cbc = 1,
    Cfb = 2,
}

public static class BlockCipherModeExtensions
{
    extension(BlockCipherMode mode)
    {
        public bool IsValid => mode is BlockCipherMode.Ecb or BlockCipherMode.Cbc or BlockCipherMode.Cfb;

        public string DisplayName => mode switch
        {
            BlockCipherMode.Ecb => "ECB",
            BlockCipherMode.Cbc => "CBC",
            BlockCipherMode.Cfb => "CFB",
            _ => "?",
        };
    }
}

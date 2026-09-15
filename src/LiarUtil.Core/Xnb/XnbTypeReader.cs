namespace LiarUtil.Core.Xnb;

public sealed record XnbTypeReader(string Name, int Version)
{
    private const string SoundEffectReaderName = "SoundEffectReader";
    private const string SpriteFontReaderName = "SpriteFontReader";

    public bool IsSoundEffect => Name.Contains(SoundEffectReaderName, StringComparison.Ordinal);

    public bool IsSpriteFont => Name.Contains(SpriteFontReaderName, StringComparison.Ordinal);
}

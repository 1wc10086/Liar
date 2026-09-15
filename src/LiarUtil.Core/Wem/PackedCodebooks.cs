namespace LiarUtil.Core.Wem;

public static class PackedCodebooks
{
    private const string DefaultResource = "LiarUtil.Core.Wem.Resources.packed_codebooks.bin";

    private const string AoTuVResource = "LiarUtil.Core.Wem.Resources.packed_codebooks_aoTuV_603.bin";

    public static byte[] LoadDefault() => Load(DefaultResource);

    public static byte[] LoadAoVu() => Load(AoTuVResource);

    private static byte[] Load(string name)
    {
        using var stream = typeof(PackedCodebooks).Assembly.GetManifestResourceStream(name)
            ?? throw new WemException(string.Format(LiarUtil.Core.Strings.BuiltInCodebookNotFound0, name));
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}

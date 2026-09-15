namespace LiarUtil.Core.Pak;

public static class PakFormat
{
    public const int PcMagic = 0x4D37BD37;

    public const int NormalMagic = unchecked((int)0xBAC04AC0);

    public const int XmemMagic = unchecked((int)0xED12F50F);

    public const int TvZipMagic = 0x04034B50;

    public const byte XorKey = 0xF7;

    public const int Version = 0;

    public const byte InfoEnd = 0x80;

    public const long DefaultFileTime = 129146222018596744L;

    public const string DefinitionFileName = "definition.json";

    public const string ResourceFolderName = "resource";

    public const string LegacyInfoFolderName = "popstudioinfo";

    public const string LegacyPackInfoFileName = "packinfo.xml";
}

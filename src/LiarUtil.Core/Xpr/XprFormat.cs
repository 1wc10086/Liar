namespace LiarUtil.Core.Xpr;

public static class XprFormat
{
    public const uint Signature = 0x58505232;
    public const int HeaderSize = 12;
    public const int EntrySize = 16;
    public const int FileAlignment = 16;
    public const int ArchiveAlignment = 2048;
    public const string DefinitionFileName = "definition.json";
    public const string ResourceFolderName = "resource";

    public static int Align(int value, int alignment) => (value + alignment - 1) / alignment * alignment;
}

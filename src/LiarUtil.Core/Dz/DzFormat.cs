namespace LiarUtil.Core.Dz;

public static class DzFormat
{
    public const string Magic = "DTRZ";

    public const byte Version = 0;

    public const ushort EndOfChunks = 0xFFFF;

    public const string DefinitionFileName = "definition.json";

    public const string ResourceFolderName = "resource";

    public const int ChunkInfoSize = 16;
}

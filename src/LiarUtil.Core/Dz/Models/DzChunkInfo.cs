namespace LiarUtil.Core.Dz.Models;

internal sealed class DzChunkInfo
{
    public ushort FolderIndex { get; set; }

    public ushort FileIndex { get; set; }

    public ushort ChunkIndex { get; set; }

    public int MultiIndex { get; set; }

    public bool IsReferenced { get; set; }

    public uint Offset { get; set; }

    public uint RecordedSize { get; set; }

    public uint Size { get; set; }

    public DzCompressionMethod Method { get; set; } = DzCompressionMethod.Store;

    public ushort ArchiveIndex { get; set; }

    public int PayloadSize { get; set; } = -1;
}

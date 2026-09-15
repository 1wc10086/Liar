namespace LiarUtil.Core.Dz.Models;

internal sealed record DzChunkPlacement(ushort FileIndex, ushort FolderIndex, ushort ChunkIndex, int MultiIndex);

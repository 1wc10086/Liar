namespace LiarUtil.Core.Rsb;

internal sealed class RsgpBuildResult
{
    public string Identifier = "";
    public uint PoolIndex;
    public uint GeneralSize;
    public uint TextureSize;
    public uint GeneralCompressedSize;
    public uint TextureCompressedSize;
    public uint TextureCount;
    public byte[] GeneralData = [];
    public byte[] TextureData = [];
    public byte[] FileList = [];
    public List<RsbTextureInfo> Textures = [];
    public List<string> Paths = [];
}

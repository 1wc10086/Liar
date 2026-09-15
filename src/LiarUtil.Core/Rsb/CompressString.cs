namespace LiarUtil.Core.Rsb;

internal sealed class CompressString
{
    public string Name;
    public int Type;
    public uint Index;
    public uint Offset;
    public uint Size;
    public uint Index2;
    public int Empty1;
    public int Empty2;
    public uint Width;
    public uint Height;

    public CompressString(string name, int type, uint index = 0, uint offset = 0, uint size = 0)
    {
        Name = name;
        Type = type;
        Index = index;
        Offset = offset;
        Size = size;
        Index2 = index;
    }
}

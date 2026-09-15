namespace LiarUtil.Core.Xnb;

[Flags]
public enum XnbFlags : byte
{
    None = 0,
    HiDef = 0x01,
    Compressed = 0x80,
}

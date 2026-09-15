namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfTexture
{
    public string Name { get; set; } = "";
    public short Cell { get; set; }
    public short Row { get; set; }
    public bool Padded { get; set; }
    public string Path { get; set; } = "";
}

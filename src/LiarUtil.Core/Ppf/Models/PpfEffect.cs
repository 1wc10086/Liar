namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfEffect
{
    public string Note { get; set; } = "";
    public PpfSize Size { get; set; } = new();
    public short FrameRate { get; set; }
    public PpfFrameRange FrameRange { get; set; } = new();
    public PpfColor BackgroundColor { get; set; } = new();
    public PpfStartupState? StartupState { get; set; }
    public List<PpfTexture> Textures { get; set; } = [];
    public List<PpfEmitter> Emitters { get; set; } = [];
    public List<PpfLayer> Layers { get; set; } = [];
    public int Unknown1 { get; set; }
    public int Unknown2 { get; set; }
    public short Unknown3 { get; set; }
    public short Unknown4 { get; set; }
    public short Unknown5 { get; set; }
    public int Unknown6 { get; set; }
    public int Unknown7 { get; set; }
    public int Unknown8 { get; set; }
    public int Unknown9 { get; set; }
    public int Unknown10 { get; set; }
    public string Unknown11 { get; set; } = "";
    public byte Unknown12 { get; set; }
    public short Unknown13 { get; set; }
    public short Unknown14 { get; set; }
}

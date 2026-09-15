namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfLayerEmitter
{
    public string Name { get; set; } = "";
    public int Type { get; set; }
    public int Geom { get; set; }
    public bool Geom4If2 { get; set; }
    public bool IsSuper { get; set; }
    public int PreloadFrame { get; set; }
    public bool EmitIn { get; set; }
    public bool EmitOut { get; set; }
    public PpfIntPoint EmitAtPoint { get; set; } = new();
    public PpfColor TintColor { get; set; } = new();
    public bool Mask { get; set; }
    public string MaskName { get; set; } = "";
    public List<string> MaskPath { get; set; } = [];
    public bool InvertMask { get; set; }
    public PpfValue2 Position { get; set; } = new();
    public List<PpfValue2> Points { get; set; } = [];
    public PpfLayerEmitterValues Values { get; set; } = new();
    public List<short> Free { get; set; } = [];
    public float Unknown1 { get; set; }
    public float Unknown2 { get; set; }
    public float Unknown3 { get; set; }
    public float Unknown4 { get; set; }
    public float Unknown5 { get; set; }
    public float Unknown6 { get; set; }
    public float Unknown7 { get; set; }
    public float Unknown8 { get; set; }
    public float Unknown9 { get; set; }
    public float Unknown10 { get; set; }
    public float Unknown11 { get; set; }
    public float Unknown12 { get; set; }
    public int Unknown13 { get; set; }
    public int Unknown14 { get; set; }
    public int Unknown15 { get; set; }
    public float Unknown16 { get; set; }
    public float Unknown17 { get; set; }
    public int Unknown18 { get; set; }
    public int Unknown19 { get; set; }
    public int Unknown20 { get; set; }
    public int Unknown21 { get; set; }
    public int Unknown22 { get; set; }
    public int Unknown23 { get; set; }
    public int Unknown24 { get; set; }
    public int Unknown25 { get; set; }
    public float Unknown26 { get; set; }
    public float Unknown27 { get; set; }
}

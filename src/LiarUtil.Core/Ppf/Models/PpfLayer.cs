using System.Text.Json.Serialization;
using LiarUtil.Core.Ppf.Json;

namespace LiarUtil.Core.Ppf.Models;

public sealed class PpfLayer
{
    public string Name { get; set; } = "";
    public List<PpfLayerEmitter> Emitters { get; set; } = [];
    public List<PpfLayerDeflector> Deflectors { get; set; } = [];
    public List<PpfLayerBlocker> Blockers { get; set; } = [];
    public List<PpfLayerForce> Forces { get; set; } = [];
    public PpfValue2 Offset { get; set; } = new();
    public PpfValue1 Angle { get; set; } = new();
    public string Unknown1 { get; set; } = "";
    public List<string> Unknown2 { get; set; } = [];
    [JsonConverter(typeof(PpfHexConverter))]
    public byte[] Unknown3 { get; set; } = [];
    [JsonConverter(typeof(PpfHexConverter))]
    public byte[] Unknown4 { get; set; } = [];
    [JsonConverter(typeof(PpfHexConverter))]
    public byte[] Unknown5 { get; set; } = [];
}

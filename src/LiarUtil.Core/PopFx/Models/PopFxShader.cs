using System.Text.Json.Serialization;
using LiarUtil.Core.PopFx.Json;

namespace LiarUtil.Core.PopFx.Models;

public sealed class PopFxShader
{
    public string Format { get; set; } = "";
    public string EntryPoint { get; set; } = "";
    [JsonConverter(typeof(PopFxCodeConverter))]
    public string Code { get; set; } = "";
    public uint CodeFormat { get; set; }
    public List<PopFxShaderParameter> Parameters { get; set; } = [];
}

using System.Text.Json.Serialization;

namespace LiarUtil.Core.PopFx.Models;

public sealed class PopFxAnnotation
{
    public string Name { get; set; } = "";
    public PopFxValue Value { get; set; } = new();
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? Unknown { get; set; }
}

using System.Text.Json.Serialization;
using LiarUtil.Core.PopFx.Json;

namespace LiarUtil.Core.PopFx.Models;

public sealed class PopFxValue
{
    [JsonConverter(typeof(PopFxValueTypeConverter))]
    public PopFxValueType Type { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Number { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float[]? Numbers { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Integer { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int[]? Integers { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint[]? Padding { get; set; }
}

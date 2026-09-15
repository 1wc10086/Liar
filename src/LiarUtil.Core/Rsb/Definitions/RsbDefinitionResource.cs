using System.Text.Json.Serialization;

namespace LiarUtil.Core.Rsb.Definitions;

internal sealed class RsbDefinitionResource
{
    public string Kind { get; set; } = RsbResourceKind.General;

    public string Path { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint Width { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint Height { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint Pitch { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint Format { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint AdditionalByteCount { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint Scale { get; set; }
}

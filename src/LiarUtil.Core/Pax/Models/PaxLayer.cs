using System.Text.Json.Serialization;

namespace LiarUtil.Core.Pax.Models;

public sealed class PaxLayer
{
    public int Index { get; set; }

    public bool Enabled { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public PaxLayerType Type { get; set; }

    public int? FootageId { get; set; }

    public string? Folder { get; set; }

    public string? FolderEncodedText { get; set; }

    public int? FolderEncodedLength { get; set; }

    public string? Name { get; set; }

    public string? NameEncodedText { get; set; }

    public int? NameEncodedLength { get; set; }

    public int? StartFrame { get; set; }

    public int? SourceDuration { get; set; }

    public int? Duration { get; set; }

    public int? Offset { get; set; }

    public bool? Additive { get; set; }

    public PaxTransformSet? Transforms { get; set; }

    public PaxFootage? Footage { get; set; }

    public int? CompositionIndex { get; set; }
}

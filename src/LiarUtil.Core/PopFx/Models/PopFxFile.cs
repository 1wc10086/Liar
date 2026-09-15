using System.Text.Json.Serialization;
using LiarUtil.Core.PopFx.Json;

namespace LiarUtil.Core.PopFx.Models;

public sealed class PopFxFile
{
    public uint Number { get; set; } = PopFxFormat.Number;
    [JsonConverter(typeof(PopFxVariantConverter))]
    public PopFxVariant Variant { get; set; } = PopFxVariant.V3;
    public List<PopFxTechnique> Techniques { get; set; } = [];
    public List<string> UnusedStrings { get; set; } = [];
}

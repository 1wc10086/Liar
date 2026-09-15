using System.Text.Json.Serialization;
using LiarUtil.Core.Pax.Json;

namespace LiarUtil.Core.Pax.Models;

[JsonConverter(typeof(PaxLayerTypeConverter))]
public enum PaxLayerType
{
    None,
    Footage,
    Composition,
}

[JsonConverter(typeof(PaxTerminatorKindConverter))]
public enum PaxTerminatorKind
{
    EndOfPopFx,
    Unknown,
}

[JsonConverter(typeof(PaxLoopTypeConverter))]
public enum PaxLoopType
{
    Repeat,
    PingPong,
}

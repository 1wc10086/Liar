using System.Text.Json.Serialization;
using LiarUtil.Core.Pak.Json;

namespace LiarUtil.Core.Pak.Models;

[JsonConverter(typeof(PakCompressionJsonConverter))]
public enum PakCompression : byte
{
    Store = 0,
    Zlib = 1,
}

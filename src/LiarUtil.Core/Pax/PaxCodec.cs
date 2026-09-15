using System.Text.Json;
using LiarUtil.Core.Pax.Decoding;
using LiarUtil.Core.Pax.Encoding;
using LiarUtil.Core.Pax.Json;
using LiarUtil.Core.Pax.Models;

namespace LiarUtil.Core.Pax;

public static class PaxCodec
{
    public static PaxFile Decode(byte[] data) => new PaxDecoder(data).Decode();

    public static byte[] Encode(PaxFile file) => new PaxEncoder().Encode(file);

    public static string DecodeToJson(byte[] data) =>
        JsonSerializer.Serialize(Decode(data), PaxJsonContext.Default.PaxFile);

    public static byte[] EncodeFromJson(string json) => Encode(
        JsonSerializer.Deserialize(json, PaxJsonContext.Default.PaxFile)
        ?? throw new PaxException(LiarUtil.Core.Strings.PAXJSONContentEmpty));
}

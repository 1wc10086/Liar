using System.Text.Json;
using LiarUtil.Core.Cfw2.Decoding;
using LiarUtil.Core.Cfw2.Encoding;
using LiarUtil.Core.Cfw2.Json;
using LiarUtil.Core.Cfw2.Models;

namespace LiarUtil.Core.Cfw2;

public static class Cfw2Codec
{
    public static Cfw2FontWidget Decode(byte[] data) => new Cfw2Decoder(data).Decode();

    public static byte[] Encode(Cfw2FontWidget widget) => new Cfw2Encoder().Encode(widget);

    public static string DecodeToJson(byte[] data) => JsonSerializer.Serialize(Decode(data), Cfw2JsonContext.Default.Cfw2FontWidget);

    public static byte[] EncodeFromJson(string json) => Encode(
        JsonSerializer.Deserialize(json, Cfw2JsonContext.Default.Cfw2FontWidget) ?? throw new InvalidDataException(LiarUtil.Core.Strings.CFW2JSONContentEmpty));
}

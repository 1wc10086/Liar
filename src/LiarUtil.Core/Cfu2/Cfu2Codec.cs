using System.Text.Json;
using LiarUtil.Core.Cfu2.Decoding;
using LiarUtil.Core.Cfu2.Encoding;
using LiarUtil.Core.Cfu2.Json;
using LiarUtil.Core.Cfu2.Models;

namespace LiarUtil.Core.Cfu2;

public static class Cfu2Codec
{
    public static Cfu2FontWidget Decode(byte[] data) => new Cfu2Decoder(data).Decode();

    public static byte[] Encode(Cfu2FontWidget widget) => new Cfu2Encoder().Encode(widget);

    public static string DecodeToJson(byte[] data) => JsonSerializer.Serialize(Decode(data), Cfu2JsonContext.Default.Cfu2FontWidget);

    public static byte[] EncodeFromJson(string json) => Encode(
        JsonSerializer.Deserialize(json, Cfu2JsonContext.Default.Cfu2FontWidget) ?? throw new InvalidDataException(LiarUtil.Core.Strings.CFU2JSONContentEmpty));
}

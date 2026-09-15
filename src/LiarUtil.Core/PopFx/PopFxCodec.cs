using System.Text.Json;
using LiarUtil.Core.PopFx.Decoding;
using LiarUtil.Core.PopFx.Encoding;
using LiarUtil.Core.PopFx.Json;
using LiarUtil.Core.PopFx.Models;

namespace LiarUtil.Core.PopFx;

public static class PopFxCodec
{
    public static PopFxFile Decode(byte[] data, PopFxVariant variant) => new PopFxDecoder(data, variant).Decode();

    public static byte[] Encode(PopFxFile file, PopFxVariant variant)
    {
        file.Variant = variant;
        return new PopFxEncoder().Encode(file);
    }

    public static string DecodeToJson(byte[] data, PopFxVariant variant) =>
        JsonSerializer.Serialize(Decode(data, variant), PopFxJsonContext.Default.PopFxFile);

    public static byte[] EncodeFromJson(string json, PopFxVariant variant)
    {
        var file = JsonSerializer.Deserialize(json, PopFxJsonContext.Default.PopFxFile)
            ?? throw new InvalidDataException(LiarUtil.Core.Strings.POPFXJSONContentEmpty);
        return Encode(file, variant);
    }
}

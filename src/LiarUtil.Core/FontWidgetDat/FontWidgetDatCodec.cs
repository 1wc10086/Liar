using System.Text.Json;
using LiarUtil.Core.FontWidgetDat.Decoding;
using LiarUtil.Core.FontWidgetDat.Encoding;
using LiarUtil.Core.FontWidgetDat.Json;
using LiarUtil.Core.FontWidgetDat.Models;

namespace LiarUtil.Core.FontWidgetDat;

public static class FontWidgetDatCodec
{
    public static FontWidgetDatFile Decode(byte[] data) => new FontWidgetDatDecoder(data).Decode();

    public static byte[] Encode(FontWidgetDatFile file) => new FontWidgetDatEncoder().Encode(file);

    public static string DecodeToJson(byte[] data) =>
        JsonSerializer.Serialize(Decode(data), FontWidgetDatJsonContext.Default.FontWidgetDatFile);

    public static byte[] EncodeFromJson(string json) => Encode(
        JsonSerializer.Deserialize(json, FontWidgetDatJsonContext.Default.FontWidgetDatFile)
        ?? throw new FontWidgetDatException(LiarUtil.Core.Strings.FontWidgetDATJSONContentEmpty));
}

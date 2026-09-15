using System.Text.Json;

namespace LiarUtil.Core.Pam;

public static class PamCodec
{
    public static PamAnimation Decode(byte[] data) => new PamReader(data).Decode();

    public static byte[] Encode(PamAnimation animation, int version)
    {
        if (version is < 1 or > 6)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.UnsupportedPAMVersion0, version));
        }
        return new PamWriter().Encode(animation, version);
    }

    public static string EncodeJson(PamAnimation animation) =>
        JsonSerializer.Serialize(animation, PamJsonContext.Default.PamAnimation);

    public static PamAnimation DecodeJson(string json) =>
        JsonSerializer.Deserialize(json, PamJsonContext.Default.PamAnimation)
            ?? throw new InvalidDataException(LiarUtil.Core.Strings.PAMJSONContentEmpty);

    public static string DecodeToJson(byte[] data) => EncodeJson(Decode(data));

    public static byte[] EncodeFromJson(string json, int version) => Encode(DecodeJson(json), version);
}

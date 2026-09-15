using System.Text.Json;
using LiarUtil.Core.Ppf.Decoding;
using LiarUtil.Core.Ppf.Encoding;
using LiarUtil.Core.Ppf.Json;
using LiarUtil.Core.Ppf.Models;

namespace LiarUtil.Core.Ppf;

public static class PpfCodec
{
    public const int Version = 1;

    public static PpfEffect Decode(byte[] data) => new PpfDecoder(data).Decode();

    public static byte[] Encode(PpfEffect effect, int version)
    {
        if (version != Version)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.UnsupportedPPFVersion0, version));
        }
        return new PpfEncoder().Encode(effect);
    }

    public static string DecodeToJson(byte[] data) => JsonSerializer.Serialize(Decode(data), PpfJsonContext.Default.PpfEffect);

    public static byte[] EncodeFromJson(string json, int version) => Encode(
        JsonSerializer.Deserialize(json, PpfJsonContext.Default.PpfEffect) ?? throw new InvalidDataException(LiarUtil.Core.Strings.PPFJSONContentEmpty),
        version);
}

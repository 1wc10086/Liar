namespace LiarUtil.Core.Newton;

public static class NewtonCodec
{
    public static string Decode(ReadOnlySpan<byte> data) => NewtonDecoder.DecodeToJson(data);

    public static byte[] Encode(string json) => NewtonEncoder.EncodeFromJson(json);
}

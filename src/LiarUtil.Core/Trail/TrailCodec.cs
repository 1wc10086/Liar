using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.PopCap;
using LiarUtil.Core.Trail.Binary;
using LiarUtil.Core.Trail.Json;

namespace LiarUtil.Core.Trail;

public static class TrailCodec
{
    public static TrailPlatform[] Platforms { get; } =
    [
        TrailPlatform.Pc, TrailPlatform.Phone32, TrailPlatform.Phone64,
        TrailPlatform.Wp, TrailPlatform.GameConsole, TrailPlatform.Tv,
    ];

    public static TrailFile Decode(byte[] data, TrailPlatform platform, ImageIdMap? images = null) =>
        TrailBinaryCodec.Decode(data, platform, images);

    public static byte[] Encode(TrailFile trail, TrailPlatform platform, bool compress = true) =>
        TrailBinaryCodec.Encode(trail, platform, compress);

    public static TrailFile DecodeAuto(byte[] data, ImageIdMap? images = null)
    {
        foreach (var platform in Platforms)
        {
            if (TryDecode(data, platform, images) is { } trail)
            {
                return trail;
            }
        }
        throw new InvalidDataException(LiarUtil.Core.Strings.UnrecognizedTrailData);
    }

    public static TrailFile? TryDecode(byte[] data, TrailPlatform platform, ImageIdMap? images = null)
    {
        try
        {
            return Decode(data, platform, images);
        }
        catch (Exception exception) when (exception is InvalidDataException or BinaryException or ArgumentOutOfRangeException or OverflowException or IOException)
        {
            return null;
        }
    }

    public static TrailFile DecodeJson(string json) => TrailJson.Deserialize(json);

    public static string EncodeJson(TrailFile trail) => TrailJson.Serialize(trail);

    public static TrailFile DecodeXml(string xml) => TrailXml.Deserialize(xml);

    public static string EncodeXml(TrailFile trail) => TrailXml.Serialize(trail);
}

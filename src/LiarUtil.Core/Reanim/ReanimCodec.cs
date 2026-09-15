using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.PopCap;
using LiarUtil.Core.Reanim.Binary;
using LiarUtil.Core.Reanim.Flash;
using LiarUtil.Core.Reanim.Json;

namespace LiarUtil.Core.Reanim;

public static class ReanimCodec
{
    public static ReanimPlatform[] Platforms { get; } =
    [
        ReanimPlatform.Pc, ReanimPlatform.Phone32, ReanimPlatform.Phone64,
        ReanimPlatform.Wp, ReanimPlatform.GameConsole, ReanimPlatform.Tv,
    ];

    public static ReanimFile Decode(byte[] data, ReanimPlatform platform, ImageIdMap? images = null) =>
        ReanimBinaryCodec.Decode(data, platform, images);

    public static byte[] Encode(ReanimFile reanim, ReanimPlatform platform, bool compress = true) =>
        ReanimBinaryCodec.Encode(reanim, platform, compress);

    public static ReanimFile DecodeAuto(byte[] data, ImageIdMap? images = null)
    {
        foreach (var platform in Platforms)
        {
            if (TryDecode(data, platform, images) is { } reanim)
            {
                return reanim;
            }
        }
        throw new InvalidDataException(LiarUtil.Core.Strings.UnrecognizedReanimData);
    }

    public static ReanimFile? TryDecode(byte[] data, ReanimPlatform platform, ImageIdMap? images = null)
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

    public static ReanimFile DecodeJson(string json) => ReanimJson.Deserialize(json);

    public static string EncodeJson(ReanimFile reanim) => ReanimJson.Serialize(reanim);

    public static ReanimFile DecodeXml(string xml) => ReanimXml.Deserialize(xml);

    public static string EncodeXml(ReanimFile reanim) => ReanimXml.Serialize(reanim);

    public static ReanimFile DecodeXfl(string path) => XflReanimDecoder.Decode(path);

    public static ReanimFile DecodeFla(string path) => XflReanimDecoder.DecodeArchive(path);

    public static bool IsFlaArchive(string path) => XflProject.IsZipXfl(path);

    public static void EncodeXfl(ReanimFile reanim, string path, XflWriterOptions? options = null) =>
        XflWriter.Write(reanim, path, options ?? new XflWriterOptions());

    public static void EncodeFla(ReanimFile reanim, string path, XflWriterOptions? options = null) =>
        FlaArchiveWriter.Write(reanim, path, options ?? new XflWriterOptions());
}

using LiarUtil.Core.Audio;

namespace LiarUtil.Core.Wem;

public static class WemToOgg
{
    public static byte[] Convert(byte[] wem, byte[] packedCodebooks, WemForcePacketFormat forcePacketFormat = WemForcePacketFormat.NoForce, bool fullSetup = false)
    {
        var file = new WemFile(wem, forcePacketFormat);
        using var stream = new MemoryStream();
        file.GenerateOgg(stream, packedCodebooks, fullSetup);
        return stream.ToArray();
    }

    public static byte[] Convert(byte[] wem, WemForcePacketFormat forcePacketFormat = WemForcePacketFormat.NoForce, bool fullSetup = false) =>
        Convert(wem, WemCodebookSelector.Select(wem, forcePacketFormat), forcePacketFormat, fullSetup);

    public static AudioData DecodeToAudio(byte[] wem, WemForcePacketFormat forcePacketFormat = WemForcePacketFormat.NoForce, bool fullSetup = false) =>
        OggDecoder.Decode(Convert(wem, forcePacketFormat, fullSetup));

    public static byte[] ConvertToWav(byte[] wem, WemForcePacketFormat forcePacketFormat = WemForcePacketFormat.NoForce, bool fullSetup = false) =>
        WavWriter.Write(DecodeToAudio(wem, forcePacketFormat, fullSetup));
}

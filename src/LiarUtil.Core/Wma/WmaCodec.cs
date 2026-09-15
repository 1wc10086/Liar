using LiarUtil.Core.Audio;

namespace LiarUtil.Core.Wma;

public static class WmaCodec
{
    public static AudioData Decode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length < 30)
        {
            throw new WmaException(LiarUtil.Core.Strings.InvalidASFHeader);
        }

        var info = AsfReader.Parse(data);
        var decoder = new WmaDecoder(info);
        var packets = AsfPackets.Read(data, info);
        if (packets.Count == 0)
        {
            return new AudioData
            {
                SampleRate = info.SampleRate,
                Channels = info.Channels,
                Samples = [],
            };
        }

        var samples = new List<short>(packets.Count * info.SampleRate / 4 * info.Channels);
        foreach (var packet in packets)
        {
            decoder.DecodeSuperframe(packet, samples);
        }

        return new AudioData
        {
            SampleRate = info.SampleRate,
            Channels = info.Channels,
            Samples = [.. samples],
        };
    }

    public static byte[] DecodeToWav(byte[] data) => WavWriter.Write(Decode(data));
}

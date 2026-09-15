using NVorbis;

namespace LiarUtil.Core.Audio;

public static class OggDecoder
{
    public static AudioData Decode(byte[] data)
    {
        using var stream = new MemoryStream(data, writable: false);
        using var reader = new VorbisReader(stream);
        var channels = reader.Channels;
        var sampleRate = reader.SampleRate;
        if (channels <= 0 || sampleRate <= 0)
        {
            throw new AudioException(LiarUtil.Core.Strings.InvalidOGGAudioParameters);
        }

        var samples = new List<short>();
        var buffer = new float[Math.Max(channels * 4096, channels * 1024)];
        while (true)
        {
            var count = reader.ReadSamples(buffer, 0, buffer.Length);
            if (count <= 0)
            {
                break;
            }

            for (var index = 0; index < count; index++)
            {
                samples.Add(WavWriter.FloatToInt16(buffer[index]));
            }
        }

        return new AudioData
        {
            SampleRate = sampleRate,
            Channels = channels,
            Samples = [.. samples],
        };
    }

    public static byte[] DecodeToWav(byte[] data) => WavWriter.Write(Decode(data));
}

using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Audio;

public static class WavWriter
{
    private const int HeaderSize = 44;
    private const short PcmFormat = 1;
    private const short BitsPerSample = 16;

    public static byte[] Write(AudioData audio) => Write(audio.Samples, audio.SampleRate, audio.Channels);

    public static byte[] Write(ReadOnlySpan<short> samples, int sampleRate, int channels)
    {
        if (channels <= 0)
        {
            throw new AudioException(LiarUtil.Core.Strings.ChannelCountInvalid);
        }

        if (samples.Length % channels != 0)
        {
            throw new AudioException(LiarUtil.Core.Strings.SampleDataLengthDoesNotMatchChannelCount);
        }

        var dataSize = samples.Length * sizeof(short);
        var blockAlign = channels * sizeof(short);
        var writer = new BufferWriter(HeaderSize + dataSize, ByteOrder.Little);
        writer.WriteUInt8((byte)'R');
        writer.WriteUInt8((byte)'I');
        writer.WriteUInt8((byte)'F');
        writer.WriteUInt8((byte)'F');
        writer.WriteUInt32((uint)(dataSize + HeaderSize - 8));
        writer.WriteUInt8((byte)'W');
        writer.WriteUInt8((byte)'A');
        writer.WriteUInt8((byte)'V');
        writer.WriteUInt8((byte)'E');
        writer.WriteUInt8((byte)'f');
        writer.WriteUInt8((byte)'m');
        writer.WriteUInt8((byte)'t');
        writer.WriteUInt8((byte)' ');
        writer.WriteUInt32(16);
        writer.WriteInt16(PcmFormat);
        writer.WriteUInt16((ushort)channels);
        writer.WriteUInt32((uint)sampleRate);
        writer.WriteUInt32((uint)(sampleRate * blockAlign));
        writer.WriteUInt16((ushort)blockAlign);
        writer.WriteInt16(BitsPerSample);
        writer.WriteUInt8((byte)'d');
        writer.WriteUInt8((byte)'a');
        writer.WriteUInt8((byte)'t');
        writer.WriteUInt8((byte)'a');
        writer.WriteUInt32((uint)dataSize);
        foreach (var sample in samples)
        {
            writer.WriteInt16(sample);
        }

        return writer.ToArray();
    }

    public static byte[] WriteFloats(ReadOnlySpan<float> samples, int sampleRate, int channels)
    {
        var buffer = new short[samples.Length];
        for (var index = 0; index < samples.Length; index++)
        {
            buffer[index] = FloatToInt16(samples[index]);
        }

        return Write(buffer, sampleRate, channels);
    }

    public static short FloatToInt16(float value)
    {
        if (float.IsNaN(value))
        {
            return 0;
        }

        if (value <= -1f)
        {
            return short.MinValue;
        }

        if (value >= 1f)
        {
            return short.MaxValue;
        }

        return (short)(value * 32768f);
    }
}

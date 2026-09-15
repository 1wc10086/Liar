using System.Buffers.Binary;
using LiarUtil.Core.Audio;

namespace LiarUtil.Core.Xnb.Audio;

internal static class PcmDecoder
{
    public static short[] Decode(ReadOnlySpan<byte> data, WaveFormat format)
    {
        var bytesPerSample = Math.Max(format.BitsPerSample / 8, 1);
        var samples = new short[data.Length / bytesPerSample];
        for (var index = 0; index < samples.Length; index++)
        {
            var source = data.Slice(index * bytesPerSample, bytesPerSample);
            samples[index] = format.BitsPerSample switch
            {
                8 => (short)((source[0] - 128) << 8),
                16 => BinaryPrimitives.ReadInt16LittleEndian(source),
                24 => (short)(SignExtend24(source) >> 8),
                32 => (short)(BinaryPrimitives.ReadInt32LittleEndian(source) >> 16),
                _ => throw new XnbException(string.Format(LiarUtil.Core.Strings.UnsupportedPCMBitDepth0, format.BitsPerSample)),
            };
        }

        return samples;
    }

    public static short[] DecodeFloat(ReadOnlySpan<byte> data, WaveFormat format)
    {
        var bytesPerSample = format.BitsPerSample / 8;
        if (bytesPerSample is not (4 or 8))
        {
            throw new XnbException(string.Format(LiarUtil.Core.Strings.UnsupportedFloatingPointBitDepth0, format.BitsPerSample));
        }

        var samples = new short[data.Length / bytesPerSample];
        for (var index = 0; index < samples.Length; index++)
        {
            var source = data.Slice(index * bytesPerSample, bytesPerSample);
            var value = bytesPerSample == sizeof(float)
                ? BinaryPrimitives.ReadSingleLittleEndian(source)
                : (float)BinaryPrimitives.ReadDoubleLittleEndian(source);
            samples[index] = WavWriter.FloatToInt16(value);
        }

        return samples;
    }

    private static int SignExtend24(ReadOnlySpan<byte> source) =>
        (source[0] | (source[1] << 8) | (source[2] << 16)) << 8 >> 8;
}

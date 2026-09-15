using NLayer;

namespace LiarUtil.Core.Audio;

internal sealed class Mp3FrameDecoder
{
    private readonly MpegFrameDecoder _decoder = new();

    private readonly float[] _buffer = new float[1152 * 2];

    public short[] Decode(IReadOnlyList<Mp3Frame> frames, int channels, int expectedSamples)
    {
        var samples = new short[expectedSamples * channels];
        var written = 0;
        foreach (var frame in frames)
        {
            frame.Reset();
            var count = _decoder.DecodeFrame(frame, _buffer, 0);
            if (count <= 0)
            {
                continue;
            }

            var remaining = Math.Min(count, samples.Length - written);
            for (var index = 0; index < remaining; index++)
            {
                samples[written + index] = WavWriter.FloatToInt16(_buffer[index]);
            }

            written += remaining;
            if (written >= samples.Length)
            {
                break;
            }
        }

        if (written < samples.Length)
        {
            Array.Resize(ref samples, written);
        }

        return samples;
    }
}

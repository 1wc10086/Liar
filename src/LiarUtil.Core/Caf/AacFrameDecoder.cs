using LiarUtil.Core.Audio;
using SharpJaad.AAC;

namespace LiarUtil.Core.Caf;

internal sealed class AacFrameDecoder
{
    private readonly Decoder _decoder;

    private readonly SampleBuffer _buffer = new();

    public AacFrameDecoder(DecoderConfig config)
    {
        _buffer.SetBigEndian(false);
        _decoder = new Decoder(config);
    }

    public int SampleRate => _buffer.SampleRate;

    public int Channels => _buffer.Channels;

    public void Decode(ReadOnlySpan<byte> frame, int channels, List<short> samples)
    {
        _decoder.DecodeFrame(frame.ToArray(), _buffer);
        var data = _buffer.Data;
        if (data.Length == 0)
        {
            return;
        }

        var sourceChannels = Math.Max(_buffer.Channels, 1);
        var targetChannels = channels > 0 ? Math.Min(channels, sourceChannels) : sourceChannels;
        var frames = data.Length / (sourceChannels * sizeof(short));
        for (var frameIndex = 0; frameIndex < frames; frameIndex++)
        {
            for (var channel = 0; channel < targetChannels; channel++)
            {
                var index = (frameIndex * sourceChannels + channel) * sizeof(short);
                samples.Add((short)(data[index] | (data[index + 1] << 8)));
            }
        }
    }
}

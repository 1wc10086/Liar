using LiarUtil.Core.Audio;

namespace LiarUtil.Core.Xm;

public static class XmCodec
{
    private const int SampleRate = 48000;
    private const int RenderBlockFrames = 1024;
    private const int MaxSeconds = 900;

    public static AudioData Decode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length == 0)
        {
            throw new XmException(LiarUtil.Core.Strings.XMDataEmpty);
        }

        var module = XmLoader.Load(data);
        var ctx = new XmContext(module, SampleRate);

        var maxFrames = SampleRate * MaxSeconds;
        var block = new float[RenderBlockFrames * 2];
        var samples = new short[SampleRate * 4];
        var count = 0;
        var frames = 0;

        while (frames < maxFrames)
        {
            var produced = ctx.GenerateSamples(block, RenderBlockFrames);
            if (produced == 0)
            {
                break;
            }

            var needed = produced * 2;
            if (count + needed > samples.Length)
            {
                var grown = new short[Math.Max(samples.Length * 2, count + needed)];
                Array.Copy(samples, grown, count);
                samples = grown;
            }

            for (var i = 0; i < needed; i++)
            {
                samples[count + i] = WavWriter.FloatToInt16(block[i]);
            }

            count += needed;
            frames += produced;

            if (ctx.Finished)
            {
                break;
            }
        }

        var final = new short[count];
        Array.Copy(samples, final, count);

        return new AudioData
        {
            SampleRate = SampleRate,
            Channels = 2,
            Samples = final,
        };
    }

    public static byte[] DecodeToWav(byte[] data) => WavWriter.Write(Decode(data));
}

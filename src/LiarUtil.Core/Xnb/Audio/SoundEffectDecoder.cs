using LiarUtil.Core.Audio;

namespace LiarUtil.Core.Xnb.Audio;

internal static class SoundEffectDecoder
{
    public static AudioData Decode(SoundEffectData sound)
    {
        var format = sound.Format;
        var samples = format.Tag switch
        {
            WaveFormatTag.Pcm => PcmDecoder.Decode(sound.Data, format),
            WaveFormatTag.IeeeFloat => PcmDecoder.DecodeFloat(sound.Data, format),
            WaveFormatTag.MsAdpcm => MsAdpcmDecoder.Decode(sound.Data, format),
            WaveFormatTag.ImaAdpcm => ImaAdpcmDecoder.Decode(sound.Data, format),
            _ => throw new XnbException(string.Format(LiarUtil.Core.Strings.UnsupportedAudioFormat0x0X4, (ushort)format.Tag)),
        };

        return new AudioData
        {
            SampleRate = format.SampleRate,
            Channels = format.Channels,
            Samples = samples,
        };
    }
}

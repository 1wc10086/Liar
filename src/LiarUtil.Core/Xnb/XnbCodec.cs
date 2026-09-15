using LiarUtil.Core.Audio;
using LiarUtil.Core.Xnb.Audio;

namespace LiarUtil.Core.Xnb;

public static class XnbCodec
{
    public static AudioData Decode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var content = XnbContainer.Read(data);
        if (!content.PrimaryReader.IsSoundEffect)
        {
            throw new XnbException(string.Format(LiarUtil.Core.Strings.XNBPrimaryObjectNotSoundEffect0, content.PrimaryReader.Name));
        }

        var sound = XnbSoundEffectReader.Read(content.Data, content.Offset, content.Length);
        return SoundEffectDecoder.Decode(sound);
    }

    public static byte[] DecodeToWav(byte[] data) => WavWriter.Write(Decode(data));
}

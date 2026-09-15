using LiarUtil.Core.Audio;

namespace LiarUtil.Core.Sps;

public static class SpsCodec
{
    public static AudioData Decode(byte[] data) => SpsContainer.Decode(data);

    public static byte[] Encode(AudioData audio) => SpsContainer.Encode(audio);

    public static byte[] DecodeToWav(byte[] data) => WavWriter.Write(Decode(data));

    public static byte[] EncodeFromWav(byte[] wav) => Encode(WavReader.Read(wav));
}

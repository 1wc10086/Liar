using LiarUtil.Core.Audio;

namespace LiarUtil.Core.Snr;

public static class SnrCodec
{
    public static AudioData Decode(byte[] data) => SnrContainer.Decode(data);

    public static byte[] Encode(AudioData audio) => SnrContainer.Encode(audio);

    public static byte[] DecodeToWav(byte[] data) => WavWriter.Write(Decode(data));

    public static byte[] EncodeFromWav(byte[] wav) => Encode(WavReader.Read(wav));
}

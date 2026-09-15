using LiarUtil.Core.Audio;

namespace LiarUtil.Core.Caf;

internal static class CafIma4
{
    private const int PacketBytes = 34;
    private const int PacketSamples = 64;

    private static readonly int[] IndexTable =
    [
        -1, -1, -1, -1, 2, 4, 6, 8,
        -1, -1, -1, -1, 2, 4, 6, 8,
    ];

    private static readonly int[] StepTable =
    [
        7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 19, 21, 23, 25, 28, 31, 34, 37, 41, 45,
        50, 55, 60, 66, 73, 80, 88, 97, 107, 118, 130, 143, 157, 173, 190, 209, 230, 253, 279, 307,
        337, 371, 408, 449, 494, 544, 598, 658, 724, 796, 876, 963, 1060, 1166, 1282, 1411, 1552, 1707, 1878, 2066,
        2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899,
        15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767,
    ];

    public static AudioData Decode(CafDescription description, byte[] data)
    {
        var channels = (int)description.Channels;
        if (description.BytesPerPacket != 0U && description.BytesPerPacket != (uint)(PacketBytes * channels))
        {
            throw new CafException(LiarUtil.Core.Strings.UnsupportedIma4PacketLayout);
        }

        if (data.Length % (PacketBytes * channels) != 0)
        {
            throw new CafException(LiarUtil.Core.Strings.Ima4PacketTruncated);
        }

        var packets = data.Length / (PacketBytes * channels);
        var samples = new short[packets * PacketSamples * channels];
        for (var packet = 0; packet < packets; packet++)
        {
            for (var channel = 0; channel < channels; channel++)
            {
                DecodePacket(data, (packet * channels + channel) * PacketBytes, samples, (packet * PacketSamples) * channels + channel, channels);
            }
        }

        return new AudioData
        {
            SampleRate = (int)Math.Round(description.SampleRate),
            Channels = channels,
            Samples = samples,
        };
    }

    private static void DecodePacket(byte[] data, int offset, short[] samples, int destinationOffset, int channels)
    {
        var predictor = ((data[offset] << 8) | data[offset + 1]) & 0xff80;
        if ((predictor & 0x8000) != 0)
        {
            predictor -= 0x10000;
        }

        var index = data[offset + 1] & 127;
        if (index > 88)
        {
            index = 88;
        }

        for (var sample = 0; sample < PacketSamples; sample++)
        {
            var packed = data[offset + 2 + (sample >> 1)];
            var code = (sample & 1) != 0 ? packed & 15 : packed >> 4;
            var step = StepTable[index];
            var delta = step >> 3;
            if ((code & 4) != 0)
            {
                delta += step;
            }

            if ((code & 2) != 0)
            {
                delta += step >> 1;
            }

            if ((code & 1) != 0)
            {
                delta += step >> 2;
            }

            predictor += (code & 8) != 0 ? -delta : delta;
            predictor = predictor switch
            {
                < -32768 => -32768,
                > 32767 => 32767,
                _ => predictor,
            };
            index += IndexTable[code];
            index = index switch
            {
                < 0 => 0,
                > 88 => 88,
                _ => index,
            };
            samples[destinationOffset + sample * channels] = (short)predictor;
        }
    }
}

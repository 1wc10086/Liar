using LiarUtil.Core.Audio;
using SharpJaad.AAC;

namespace LiarUtil.Core.Caf;

internal static class CafAac
{
    private static readonly int[] SampleRates =
    [
        96000, 88200, 64000, 48000, 44100, 32000, 24000, 22050, 16000, 12000, 11025, 8000, 7350,
    ];

    public static AudioData Decode(CafDescription description, byte[] data, byte[] cookie, byte[] packetTable)
    {
        var channels = (int)description.Channels;
        var specificInfo = ExtractSpecificInfo(cookie) ?? SynthesizeSpecificInfo((int)Math.Round(description.SampleRate), channels);
        var config = DecoderConfig.ParseMP4DecoderSpecificInfo(specificInfo);
        var decoder = new AacFrameDecoder(config);
        var sizes = CafReader.ReadPacketSizes(description, data.Length, packetTable);
        var offset = 0;
        var samples = new List<short>();
        foreach (var size in sizes)
        {
            if (size <= 0 || offset + size > data.Length)
            {
                break;
            }

            decoder.Decode(data.AsSpan(offset, size), channels, samples);
            offset += size;
        }

        var sampleRate = decoder.SampleRate > 0 ? decoder.SampleRate : (int)Math.Round(description.SampleRate);
        var outputChannels = decoder.Channels > 0 ? Math.Min(channels, decoder.Channels) : channels;
        return new AudioData
        {
            SampleRate = sampleRate,
            Channels = outputChannels,
            Samples = [.. samples],
        };
    }

    private static byte[]? ExtractSpecificInfo(byte[] cookie)
    {
        if (cookie.Length < 2 || cookie[0] != 0x03)
        {
            return null;
        }

        var offset = 1;
        var size = ReadLength(cookie, ref offset);
        var end = offset + size;
        if (end > cookie.Length)
        {
            return null;
        }

        var flags = cookie[offset + 2];
        offset += 3;
        if ((flags & 0x80) != 0)
        {
            offset += 2;
        }

        if ((flags & 0x40) != 0)
        {
            if (offset >= end)
            {
                return null;
            }

            offset += 1 + cookie[offset];
        }

        if ((flags & 0x20) != 0)
        {
            offset += 2;
        }

        if (offset + 2 > end || cookie[offset] != 0x04)
        {
            return null;
        }

        offset++;
        ReadLength(cookie, ref offset);
        offset += 13;
        if (offset + 2 > end || cookie[offset] != 0x05)
        {
            return null;
        }

        offset++;
        var length = ReadLength(cookie, ref offset);
        return offset + length <= end ? cookie[offset..(offset + length)] : null;
    }

    private static int ReadLength(byte[] data, ref int offset)
    {
        var value = 0;
        for (var index = 0; index < 4; index++)
        {
            if (offset >= data.Length)
            {
                return value;
            }

            var current = data[offset++];
            value = (value << 7) | (current & 0x7F);
            if ((current & 0x80) == 0)
            {
                break;
            }
        }

        return value;
    }

    private static byte[] SynthesizeSpecificInfo(int sampleRate, int channels)
    {
        var sampleRateIndex = Array.IndexOf(SampleRates, sampleRate);
        if (sampleRateIndex < 0)
        {
            throw new CafException(string.Format(LiarUtil.Core.Strings.UnsupportedAACSampleRate0, sampleRate));
        }

        var specificInfo = new byte[2];
        specificInfo[0] = (byte)(2 << 3);
        specificInfo[0] |= (byte)((sampleRateIndex >> 1) & 0x7);
        specificInfo[1] = (byte)((sampleRateIndex & 0x1) << 7);
        specificInfo[1] |= (byte)((channels & 0x7) << 3);
        return specificInfo;
    }
}

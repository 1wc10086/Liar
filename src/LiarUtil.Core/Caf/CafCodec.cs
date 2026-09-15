using System.Buffers.Binary;
using LiarUtil.Core.Audio;

namespace LiarUtil.Core.Caf;

public static class CafCodec
{
    private const uint LpcmFloat = 1;
    private const uint LpcmBigEndian = 2;
    private const uint LpcmSignedInteger = 4;
    private const uint LpcmPacked = 8;
    private const uint LpcmAlignedHigh = 16;
    private const uint LpcmNonInterleaved = 32;

    public static AudioData Decode(byte[] data)
    {
        var (description, payload, cookie, packetTable) = CafReader.Parse(data);
        return description.Format switch
        {
            "lpcm" => DecodeLpcm(description, payload),
            "ulaw" => DecodeG711(description, payload, MuLaw),
            "alaw" => DecodeG711(description, payload, ALaw),
            "ima4" => CafIma4.Decode(description, payload),
            "aac " => CafAac.Decode(description, payload, cookie, packetTable),
            _ => throw new CafException(string.Format(LiarUtil.Core.Strings.UnsupportedCAFAudioFormat0, description.Format)),
        };
    }

    public static byte[] DecodeToWav(byte[] data) => WavWriter.Write(Decode(data));

    private static AudioData DecodeLpcm(CafDescription description, byte[] data)
    {
        var bits = (int)description.Bits;
        var bytesPerSample = (bits + 7) / 8;
        if (bytesPerSample == 0 || bits > 64)
        {
            throw new CafException(LiarUtil.Core.Strings.InvalidLPCMSampleLayout);
        }

        if ((description.Flags & LpcmNonInterleaved) != 0U)
        {
            throw new CafException(LiarUtil.Core.Strings.NonInterleavedLPCMCAFNotSupported);
        }

        if (description.BytesPerPacket != 0U && description.FramesPerPacket != 0U)
        {
            var samplesPerPacket = description.FramesPerPacket * description.Channels;
            if (samplesPerPacket == 0 || description.BytesPerPacket % samplesPerPacket != 0)
            {
                throw new CafException(LiarUtil.Core.Strings.UnsupportedLPCMPacketLayout);
            }

            bytesPerSample = (int)(description.BytesPerPacket / samplesPerPacket);
        }

        if (bytesPerSample != (bits + 7) / 8
            || ((description.Flags & LpcmPacked) == 0U && bits % 8 != 0)
            || (description.Flags & LpcmAlignedHigh) != 0U)
        {
            throw new CafException(LiarUtil.Core.Strings.PaddedLPCMSamplesAreNotSupported);
        }

        var channels = (int)description.Channels;
        if (data.Length % (bytesPerSample * channels) != 0)
        {
            throw new CafException(LiarUtil.Core.Strings.LPCMDataDoesNotContainCompleteFrame);
        }

        var bigEndian = (description.Flags & LpcmBigEndian) != 0U;
        var floating = (description.Flags & LpcmFloat) != 0U;
        var signed = (description.Flags & LpcmSignedInteger) != 0U;
        var samples = new short[data.Length / bytesPerSample];
        for (var index = 0; index < samples.Length; index++)
        {
            var offset = index * bytesPerSample;
            var value = floating
                ? ReadFloat(data, offset, bits, bigEndian)
                : ReadInteger(data, offset, bits, bigEndian, signed);
            samples[index] = WavWriter.FloatToInt16(value);
        }

        return new AudioData
        {
            SampleRate = (int)Math.Round(description.SampleRate),
            Channels = channels,
            Samples = samples,
        };
    }

    private static float ReadFloat(byte[] data, int offset, int bits, bool bigEndian)
    {
        return bits switch
        {
            32 => ReadSingle(data, offset, bigEndian),
            64 => (float)ReadDouble(data, offset, bigEndian),
            _ => throw new CafException(LiarUtil.Core.Strings.UnsupportedLPCMFloatingPointBitDepth),
        };
    }

    private static float ReadInt8(byte value, bool signed) => signed ? (sbyte)value / 128f : (value - 128) / 128f;

    private static float ReadInteger(byte[] data, int offset, int bits, bool bigEndian, bool signed)
    {
        switch (bits)
        {
            case 8:
                return ReadInt8(data[offset], signed);
            case 16:
            {
                var raw = ReadUInt16(data, offset, bigEndian);
                return (signed ? (short)raw : raw - 32768) / 32768f;
            }
            case 24:
            {
                var raw = ReadUInt24(data, offset, bigEndian);
                if (signed && raw >= 0x800000)
                {
                    raw -= 0x1000000;
                }

                return (signed ? raw : raw - 0x800000) / 8388608f;
            }
            case 32:
            {
                var raw = ReadUInt32(data, offset, bigEndian);
                if (signed && raw >= 0x80000000)
                {
                    raw -= 0x100000000L;
                }

                return (signed ? raw : raw - 0x80000000L) / 2147483648f;
            }
            default:
                throw new CafException(LiarUtil.Core.Strings.UnsupportedLPCMIntegerBitDepth);
        }
    }

    private static ushort ReadUInt16(byte[] data, int offset, bool bigEndian) => bigEndian
        ? BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset))
        : BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset));

    private static int ReadUInt24(byte[] data, int offset, bool bigEndian) => bigEndian
        ? data[offset] << 16 | data[offset + 1] << 8 | data[offset + 2]
        : data[offset + 2] << 16 | data[offset + 1] << 8 | data[offset];

    private static long ReadUInt32(byte[] data, int offset, bool bigEndian) => bigEndian
        ? BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset))
        : BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset));

    private static AudioData DecodeG711(CafDescription description, byte[] data, Func<byte, float> decode)
    {
        var channels = (int)description.Channels;
        if (data.Length % channels != 0)
        {
            throw new CafException(LiarUtil.Core.Strings.G711DataDoesNotContainCompleteFrame);
        }

        var samples = new short[data.Length];
        for (var index = 0; index < samples.Length; index++)
        {
            samples[index] = WavWriter.FloatToInt16(decode(data[index]));
        }

        return new AudioData
        {
            SampleRate = (int)Math.Round(description.SampleRate),
            Channels = channels,
            Samples = samples,
        };
    }

    private static float MuLaw(byte value)
    {
        var inverted = ~value & 255;
        var magnitude = ((inverted & 15) << 3) + 132;
        magnitude <<= (inverted >> 4) & 7;
        return ((inverted & 128) != 0 ? 132 - magnitude : magnitude - 132) / 32768f;
    }

    private static float ALaw(byte value)
    {
        var inverted = value ^ 85;
        var magnitude = (inverted & 15) << 4;
        var exponent = (inverted >> 4) & 7;
        magnitude += exponent != 0 ? 264 : 8;
        if (exponent > 1)
        {
            magnitude <<= exponent - 1;
        }

        return ((inverted & 128) != 0 ? magnitude : -magnitude) / 32768f;
    }

    private static float ReadSingle(byte[] data, int offset, bool bigEndian) => bigEndian
        ? BinaryPrimitives.ReadSingleBigEndian(data.AsSpan(offset))
        : BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(offset));

    private static double ReadDouble(byte[] data, int offset, bool bigEndian) => bigEndian
        ? BinaryPrimitives.ReadDoubleBigEndian(data.AsSpan(offset))
        : BinaryPrimitives.ReadDoubleLittleEndian(data.AsSpan(offset));
}

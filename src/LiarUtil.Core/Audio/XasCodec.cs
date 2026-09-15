using System.Buffers.Binary;

namespace LiarUtil.Core.Audio;

public static class XasCodec
{
    public static int EncodedSize(int sampleCountPerChannel, int channels) =>
        ChunkCount(sampleCountPerChannel) * channels * XasConstants.ChunkSize;

    public static short[] Decode(ReadOnlySpan<byte> data, int sampleCountPerChannel, int channels)
    {
        if (sampleCountPerChannel <= 0 || channels <= 0)
        {
            return [];
        }

        var required = EncodedSize(sampleCountPerChannel, channels);
        if (data.Length < required)
        {
            throw new AudioException(LiarUtil.Core.Strings.XASDataTruncated);
        }

        var output = new short[sampleCountPerChannel * channels];
        var chunkCount = ChunkCount(sampleCountPerChannel);
        var source = 0;
        for (var chunk = 0; chunk < chunkCount; chunk++)
        {
            var count = Math.Min(XasConstants.SamplesPerChunk, sampleCountPerChannel - chunk * XasConstants.SamplesPerChunk);
            for (var channel = 0; channel < channels; channel++)
            {
                DecodeChunk(data.Slice(source, XasConstants.ChunkSize), output, chunk * XasConstants.SamplesPerChunk, channel, channels, count);
                source += XasConstants.ChunkSize;
            }
        }

        return output;
    }

    public static byte[] Encode(ReadOnlySpan<short> pcm, int sampleCountPerChannel, int channels)
    {
        if (sampleCountPerChannel <= 0 || channels <= 0)
        {
            return [];
        }

        var output = new byte[EncodedSize(sampleCountPerChannel, channels)];
        var block = new short[XasConstants.SamplesPerChunk];
        var chunkCount = ChunkCount(sampleCountPerChannel);
        var destination = 0;
        for (var chunk = 0; chunk < chunkCount; chunk++)
        {
            var count = Math.Min(XasConstants.SamplesPerChunk, sampleCountPerChannel - chunk * XasConstants.SamplesPerChunk);
            for (var channel = 0; channel < channels; channel++)
            {
                block.AsSpan().Clear();
                for (var index = 0; index < count; index++)
                {
                    block[index] = pcm[(chunk * XasConstants.SamplesPerChunk + index) * channels + channel];
                }

                EncodeChunk(block, output.AsSpan(destination, XasConstants.ChunkSize));
                destination += XasConstants.ChunkSize;
            }
        }

        return output;
    }

    public static void DecodeChunk(ReadOnlySpan<byte> chunk, short[] output, int destinationOffset, int channel, int channels, int count)
    {
        for (var subChunk = 0; subChunk < XasConstants.SubChunkCount; subChunk++)
        {
            var header1 = BinaryPrimitives.ReadUInt16LittleEndian(chunk[(subChunk * 4)..]);
            var header2 = BinaryPrimitives.ReadUInt16LittleEndian(chunk[(subChunk * 4 + 2)..]);
            var coefficient = XasConstants.Coefficients[header1 & 0b11];
            var shift = 20 - (header2 & 0b1111);
            var previous0 = Sign12(header1 >> 4) * 16;
            var previous1 = Sign12(header2 >> 4) * 16;
            var baseIndex = subChunk * XasConstants.SamplesPerSubChunk;
            if (baseIndex < count)
            {
                output[(destinationOffset + baseIndex) * channels + channel] = (short)previous0;
            }

            if (baseIndex + 1 < count)
            {
                output[(destinationOffset + baseIndex + 1) * channels + channel] = (short)previous1;
            }

            for (var pair = 0; pair < 15; pair++)
            {
                var packed = chunk[16 + pair * XasConstants.SubChunkCount + subChunk];
                for (var nibble = 0; nibble < 2; nibble++)
                {
                    var correction = Sign4(nibble == 0 ? packed >> 4 : packed) << shift;
                    var prediction = previous1 * coefficient[0] + previous0 * coefficient[1];
                    var decoded = ClipInt16((prediction + correction + 128) >> 8);
                    var index = baseIndex + 2 + pair * 2 + nibble;
                    if (index < count)
                    {
                        output[(destinationOffset + index) * channels + channel] = decoded;
                    }

                    previous0 = previous1;
                    previous1 = decoded;
                }
            }
        }
    }

    public static void EncodeChunk(ReadOnlySpan<short> samples, Span<byte> destination)
    {
        var decoded = new short[XasConstants.SamplesPerSubChunk];
        for (var subChunk = 0; subChunk < XasConstants.SubChunkCount; subChunk++)
        {
            var baseIndex = subChunk * XasConstants.SamplesPerSubChunk;
            var first = (samples[baseIndex] + 7) >> 4;
            var second = (samples[baseIndex + 1] + 7) >> 4;
            decoded[0] = (short)(first << 4);
            decoded[1] = (short)(second << 4);
            var (coefficientIndex, exponent) = ChooseParameters(samples.Slice(baseIndex, XasConstants.SamplesPerSubChunk), decoded[0], decoded[1]);
            BinaryPrimitives.WriteUInt16LittleEndian(destination[(subChunk * 4)..], (ushort)(((first & 4095) << 4) | coefficientIndex));
            BinaryPrimitives.WriteUInt16LittleEndian(destination[(subChunk * 4 + 2)..], (ushort)(((second & 4095) << 4) | exponent));
            var coefficient = XasConstants.Coefficients[coefficientIndex];
            var shift = 20 - exponent;
            for (var pair = 0; pair < 15; pair++)
            {
                var firstSample = EncodeSample(decoded[pair * 2], decoded[pair * 2 + 1], coefficient, samples[baseIndex + 2 + pair * 2], shift);
                decoded[pair * 2 + 2] = firstSample.Decoded;
                var secondSample = EncodeSample(decoded[pair * 2 + 1], firstSample.Decoded, coefficient, samples[baseIndex + 3 + pair * 2], shift);
                decoded[pair * 2 + 3] = secondSample.Decoded;
                destination[16 + pair * XasConstants.SubChunkCount + subChunk] =
                    (byte)(((firstSample.Encoded & 15) << 4) | (secondSample.Encoded & 15));
            }
        }
    }

    private static (int Coefficient, byte Exponent) ChooseParameters(ReadOnlySpan<short> samples, int previous0, int previous1)
    {
        var bestCoefficient = 0;
        var minimum = int.MaxValue;
        for (var coefficientIndex = 0; coefficientIndex < XasConstants.Coefficients.Length; coefficientIndex++)
        {
            var coefficient = XasConstants.Coefficients[coefficientIndex];
            var old = previous0;
            var recent = previous1;
            var maximum = 0;
            for (var index = 0; index < 30; index++)
            {
                var sample = samples[index + 2];
                var error = (sample << 8) - coefficient[0] * recent - coefficient[1] * old;
                var absolute = Math.Abs(error);
                if (absolute > maximum)
                {
                    maximum = absolute;
                }

                old = recent;
                recent = sample;
            }

            if (maximum < minimum)
            {
                minimum = maximum;
                bestCoefficient = coefficientIndex;
            }
        }

        var clipped = ClipInt16(minimum >> 8);
        var mask = 0x4000;
        var exponent = 0;
        for (; exponent < 12; exponent++)
        {
            if ((((mask >> 3) + clipped) & mask) != 0)
            {
                break;
            }

            mask >>= 1;
        }

        return (bestCoefficient, (byte)exponent);
    }

    private static (int Encoded, short Decoded) EncodeSample(int previous0, int previous1, short[] coefficient, short sample, int shift)
    {
        var prediction = previous1 * coefficient[0] + previous0 * coefficient[1];
        var rounding = 1 << (shift - 1);
        var encoded = ClipInt4(((sample << 8) - prediction + rounding) >> shift);
        var predecoded = ((encoded << shift) + prediction + 128) >> 8;
        var decoded = ClipInt16(predecoded);
        var term = 1 << (shift - 8);
        var alternative = ClipInt16(predecoded + term);
        if (encoded != 7 && Math.Abs(decoded - sample) > Math.Abs(alternative - sample))
        {
            encoded++;
            decoded = alternative;
        }
        else
        {
            alternative = ClipInt16(predecoded - term);
            if (encoded != -8 && Math.Abs(decoded - sample) > Math.Abs(alternative - sample))
            {
                encoded--;
                decoded = alternative;
            }
        }

        return (encoded, decoded);
    }

    private static int ChunkCount(int sampleCount) => (sampleCount + XasConstants.SamplesPerChunk - 1) / XasConstants.SamplesPerChunk;

    private static int Sign4(int value) => (value & 8) != 0 ? (value & 15) - 16 : value & 15;

    private static int Sign12(int value) => (value & 2048) != 0 ? value - 4096 : value;

    private static int ClipInt4(int value) => value >= 7 ? 7 : value <= -8 ? -8 : value;

    private static short ClipInt16(int value) => (short)(value >= 32767 ? 32767 : value <= -32768 ? -32768 : value);
}

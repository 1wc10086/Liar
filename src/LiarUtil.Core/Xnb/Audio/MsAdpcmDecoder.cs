using System.Buffers.Binary;

namespace LiarUtil.Core.Xnb.Audio;

internal static class MsAdpcmDecoder
{
    private const int StateLength = 7;

    private static readonly int[] AdaptationTable =
    [
        230, 230, 230, 230, 307, 409, 512, 614, 768, 614, 512, 409, 307, 230, 230, 230,
    ];

    public static short[] Decode(ReadOnlySpan<byte> data, WaveFormat format)
    {
        var channels = format.Channels;
        var blockAlign = format.BlockAlign;
        if (blockAlign < StateLength * channels)
        {
            throw new XnbException(LiarUtil.Core.Strings.InvalidMSADPCMBlockAlignment);
        }

        var coefficients = format.Coefficients.Count > 0
            ? format.Coefficients
            : WaveFormatParser.DefaultAdpcmCoefficients;
        var samplesPerBlock = (blockAlign - StateLength * channels) * 2 / channels + 2;
        var result = new short[(data.Length / blockAlign + 1) * samplesPerBlock * channels];
        var states = new MsAdpcmChannelState[channels];
        var written = 0;
        for (var offset = 0; offset + StateLength * channels <= data.Length;)
        {
            var block = data.Slice(offset, Math.Min(blockAlign, data.Length - offset));
            offset += block.Length;
            written += DecodeBlock(block, channels, coefficients, states, result, written);
        }

        return result.AsSpan(0, written).ToArray();
    }

    private static int DecodeBlock(
        ReadOnlySpan<byte> block,
        int channels,
        IReadOnlyList<AdpcmCoefficient> coefficients,
        MsAdpcmChannelState[] states,
        short[] destination,
        int start)
    {
        var position = 0;
        for (var channel = 0; channel < channels; channel++)
        {
            states[channel].Predictor = block[position++];
        }

        for (var channel = 0; channel < channels; channel++)
        {
            states[channel].Delta = BinaryPrimitives.ReadInt16LittleEndian(block[position..]);
            position += sizeof(short);
        }

        for (var channel = 0; channel < channels; channel++)
        {
            states[channel].Previous = BinaryPrimitives.ReadInt16LittleEndian(block[position..]);
            position += sizeof(short);
        }

        for (var channel = 0; channel < channels; channel++)
        {
            states[channel].Older = BinaryPrimitives.ReadInt16LittleEndian(block[position..]);
            position += sizeof(short);
        }

        for (var channel = 0; channel < channels; channel++)
        {
            destination[start + channel] = (short)states[channel].Older;
        }

        for (var channel = 0; channel < channels; channel++)
        {
            destination[start + channels + channel] = (short)states[channel].Previous;
        }

        var written = channels * 2;
        var nibbleCount = (block.Length - position) * 2;
        for (var nibbleIndex = 0; nibbleIndex < nibbleCount; nibbleIndex++)
        {
            var packed = block[position + nibbleIndex / 2];
            var nibble = (nibbleIndex & 1) == 0 ? packed >> 4 : packed & 0x0F;
            ref var state = ref states[nibbleIndex % channels];
            if (state.Predictor >= coefficients.Count)
            {
                throw new XnbException(string.Format(LiarUtil.Core.Strings.MSADPCMPredictorCoefficientIndexOutOfRange, state.Predictor));
            }

            var coefficient = coefficients[state.Predictor];
            var value = (state.Previous * coefficient.Coefficient1 + state.Older * coefficient.Coefficient2) / 256;
            value += (nibble >= 8 ? nibble - 16 : nibble) * state.Delta;
            value = Math.Clamp(value, short.MinValue, short.MaxValue);
            state.Older = state.Previous;
            state.Previous = value;
            state.Delta = AdaptationTable[nibble] * state.Delta >> 8;
            if (state.Delta < 16)
            {
                state.Delta = 16;
            }

            destination[start + written++] = (short)value;
        }

        return written;
    }
}

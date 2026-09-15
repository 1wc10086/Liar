using System.Buffers.Binary;

namespace LiarUtil.Core.Xnb.Audio;

internal static class ImaAdpcmDecoder
{
    private const int StateLength = 4;
    private const int GroupSamples = 8;

    private static readonly int[] StepTable =
    [
        7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 19, 21, 23, 25, 28, 31, 34, 37, 41, 45,
        50, 55, 60, 66, 73, 80, 88, 97, 107, 118, 130, 143, 157, 173, 190, 209, 230, 253, 279, 307,
        337, 371, 408, 449, 494, 544, 598, 658, 724, 796, 876, 963, 1060, 1166, 1282, 1411, 1552, 1707, 1878, 2066,
        2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899,
        15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767,
    ];

    private static readonly int[] IndexTable =
    [
        -1, -1, -1, -1, 2, 4, 6, 8, -1, -1, -1, -1, 2, 4, 6, 8,
    ];

    public static short[] Decode(ReadOnlySpan<byte> data, WaveFormat format)
    {
        var channels = format.Channels;
        var blockAlign = format.BlockAlign;
        if (blockAlign < StateLength * channels)
        {
            throw new XnbException(LiarUtil.Core.Strings.InvalidIMAADPCMBlockAlignment);
        }

        var samplesPerBlock = (blockAlign - StateLength * channels) * 2 / channels + 1;
        var result = new short[(data.Length / blockAlign + 1) * samplesPerBlock * channels];
        var states = new ImaAdpcmChannelState[channels];
        var written = 0;
        for (var offset = 0; offset + StateLength * channels <= data.Length;)
        {
            var block = data.Slice(offset, Math.Min(blockAlign, data.Length - offset));
            offset += block.Length;
            written += DecodeBlock(block, channels, states, result, written);
        }

        return result.AsSpan(0, written).ToArray();
    }

    private static int DecodeBlock(
        ReadOnlySpan<byte> block,
        int channels,
        ImaAdpcmChannelState[] states,
        short[] destination,
        int start)
    {
        var position = 0;
        for (var channel = 0; channel < channels; channel++)
        {
            states[channel].Sample = BinaryPrimitives.ReadInt16LittleEndian(block[position..]);
            states[channel].StepIndex = Math.Clamp(block[position + 2], 0, StepTable.Length - 1);
            position += StateLength;
        }

        var written = 0;
        for (var channel = 0; channel < channels; channel++)
        {
            destination[start + written++] = (short)states[channel].Sample;
        }

        Span<short> group = stackalloc short[channels * GroupSamples];
        while (position + channels * StateLength <= block.Length)
        {
            for (var channel = 0; channel < channels; channel++)
            {
                for (var index = 0; index < GroupSamples; index++)
                {
                    var packed = block[position + channel * StateLength + index / 2];
                    var nibble = (index & 1) == 0 ? packed & 0x0F : packed >> 4;
                    group[index * channels + channel] = Expand(nibble, ref states[channel]);
                }
            }

            position += channels * StateLength;
            for (var index = 0; index < GroupSamples; index++)
            {
                for (var channel = 0; channel < channels; channel++)
                {
                    destination[start + written++] = group[index * channels + channel];
                }
            }
        }

        return written;
    }

    private static short Expand(int nibble, ref ImaAdpcmChannelState state)
    {
        var step = StepTable[state.StepIndex];
        var difference = step >> 3;
        if ((nibble & 1) != 0)
        {
            difference += step >> 2;
        }

        if ((nibble & 2) != 0)
        {
            difference += step >> 1;
        }

        if ((nibble & 4) != 0)
        {
            difference += step;
        }

        state.Sample = (nibble & 8) != 0 ? state.Sample - difference : state.Sample + difference;
        state.Sample = Math.Clamp(state.Sample, short.MinValue, short.MaxValue);
        state.StepIndex = Math.Clamp(state.StepIndex + IndexTable[nibble], 0, StepTable.Length - 1);
        return (short)state.Sample;
    }
}

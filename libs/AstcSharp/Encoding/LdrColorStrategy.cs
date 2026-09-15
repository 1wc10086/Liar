using System.Runtime.CompilerServices;
using AstcSharp.ColorEncoding;
using AstcSharp.Core;

namespace AstcSharp.Encoding;

/// <summary>
/// The LDR (<see cref="RgbaColor"/>, byte channels) implementation. Principal-axis endpoint fitting via
/// <see cref="EndpointFitter"/>, LDR endpoint encoding via <see cref="EndpointEncoder"/>, and the
/// decoder's LDR interpolation (spec §C.2.19) for reconstruction.
/// </summary>
internal readonly struct LdrColorStrategy : IColorSpaceStrategy<RgbaColor>
{
    private const int ChannelCount = BlockInfo.ChannelsPerPixel;

    public long EarlyOutPerSampleError => FastSearchPolicy.LdrEarlyOutPerSampleError;

    public (RgbaColor Low, RgbaColor High) Fit(ReadOnlySpan<RgbaColor> texels)
        => EndpointFitter.Fit(texels);

    public bool FitSubsets(ReadOnlySpan<RgbaColor> texels, ReadOnlySpan<int> assignment, int partitionCount, Span<RgbaColor> subsetLow, Span<RgbaColor> subsetHigh)
        => EndpointFitter.FitSubsets(texels, assignment, partitionCount, subsetLow, subsetHigh);

    public int SelectCandidateModes(ReadOnlySpan<RgbaColor> texels, Span<ColorEndpointMode> modes)
    {
        bool opaque = true;
        bool grey = true;
        foreach (RgbaColor texel in texels)
        {
            opaque &= texel.A == byte.MaxValue;
            grey &= texel.R == texel.G && texel.G == texel.B;
        }

        int count = 0;
        if (grey)
        {
            if (opaque)
            {
                modes[count++] = ColorEndpointMode.LdrLumaDirect;
                modes[count++] = ColorEndpointMode.LdrLumaBaseOffset;
            }
            else
            {
                modes[count++] = ColorEndpointMode.LdrLumaAlphaDirect;
                modes[count++] = ColorEndpointMode.LdrLumaAlphaBaseOffset;
            }
        }
        else if (opaque)
        {
            modes[count++] = ColorEndpointMode.LdrRgbDirect;
            modes[count++] = ColorEndpointMode.LdrRgbBaseScale;
        }
        else
        {
            modes[count++] = ColorEndpointMode.LdrRgbaDirect;
            modes[count++] = ColorEndpointMode.LdrRgbaBaseOffset;
        }

        return count;
    }

    public void EncodeEndpoints(ColorEndpointMode mode, RgbaColor low, RgbaColor high, int colorRange, Span<int> colorValues)
        => EndpointEncoder.Encode(mode, low, high, colorRange, colorValues);

    public void StoreEffectiveChannels(in ColorEndpointPair pair, Span<int> low, Span<int> high)
    {
        StoreChannels(pair.LdrLow, low);
        StoreChannels(pair.LdrHigh, high);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StoreChannels(RgbaColor endpoint, Span<int> channels)
    {
        for (int channel = 0; channel < ChannelCount; channel++)
        {
            channels[channel] = endpoint.GetChannel(channel);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetChannel(RgbaColor texel, int channel) => texel.GetChannel(channel);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Reconstruct(int low, int high, int weight)
        => (Interpolation.BlendLdrReplicated(low, high, weight) >> 8) & byte.MaxValue;
}

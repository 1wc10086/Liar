using System.Collections.Concurrent;
using AstcSharp.BiseEncoding;
using AstcSharp.BlockDecoding;
using AstcSharp.Core;

namespace AstcSharp.Encoding;

internal readonly record struct EncodingConfig(int WeightRange, int ColorRange);

internal sealed record EncodingGrid(int Width, int Height, DecimationInfo Decimation, EncodingConfig[] Configurations)
{
    public int WeightCount => Width * Height;
}

internal static class EncodingConfigTable
{
    private static readonly ConcurrentDictionary<(FootprintType, int, int, bool), EncodingGrid[]> Tables = new();

    public static EncodingGrid[] Get(Footprint footprint, int colorValues, int colorStartBit, bool dual) =>
        Tables.GetOrAdd((footprint.Type, colorValues, colorStartBit, dual), static key => Build(key.Item1, key.Item2, key.Item3, key.Item4));

    private static EncodingGrid[] Build(FootprintType type, int colorValues, int colorStartBit, bool dual)
    {
        var footprint = Footprint.FromFootprintType(type);
        var grids = new List<EncodingGrid>();
        ReadOnlySpan<int> ranges = [31, 23, 19, 15, 11, 9, 7, 5, 4, 3, 2, 1];
        int[] heights = dual ? Enumerable.Range(2, footprint.Height - 1).ToArray() : [footprint.Height, 2];
        int[] widths = dual ? Enumerable.Range(2, footprint.Width - 1).ToArray() : [footprint.Width, 2];
        foreach (var height in heights)
        {
            foreach (var width in widths)
            {
                var weights = width * height * (dual ? 2 : 1);
                if (weights > 64)
                {
                    continue;
                }

                var configs = new List<EncodingConfig>();
                foreach (var range in ranges)
                {
                    if (!BlockModeEncoder.TryEncode(width, height, range, dual, out _))
                    {
                        continue;
                    }

                    var weightBits = BoundedIntegerSequenceCodec.GetBitCountForRange(weights, range);
                    var colorBits = 128 - weightBits - colorStartBit - (dual ? 2 : 0);
                    if (weightBits is >= 24 and <= 96 &&
                        BlockModeDecoder.TryResolveColorEncoding(colorValues, colorBits, out var colorRange, out _))
                    {
                        configs.Add(new EncodingConfig(range, colorRange));
                    }
                }

                if (configs.Count > 0)
                {
                    grids.Add(new EncodingGrid(width, height, DecimationTable.Get(footprint, width, height), configs.ToArray()));
                }
            }
        }

        return grids.ToArray();
    }
}

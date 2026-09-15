using AstcSharp.BlockDecoding;

namespace AstcSharp.Encoding;

internal static class BlockModeEncoder
{
    private static readonly ushort[] Modes = Build();

    public static ushort Encode(int gridWidth, int gridHeight, int weightRange, bool isDualPlane) =>
        TryEncode(gridWidth, gridHeight, weightRange, isDualPlane, out var bits)
            ? bits
            : throw new ArgumentException($"Invalid ASTC weight configuration: {gridWidth}x{gridHeight}/{weightRange}/{isDualPlane}");

    public static bool TryEncode(int gridWidth, int gridHeight, int weightRange, bool isDualPlane, out ushort modeBits)
    {
        modeBits = 0;
        if (gridWidth is < 2 or > 12 || gridHeight is < 2 or > 12 || weightRange is < 1 or > 31)
        {
            return false;
        }

        var stored = Modes[Index(gridWidth, gridHeight, weightRange, isDualPlane)];
        if (stored == 0)
        {
            return false;
        }

        modeBits = (ushort)(stored - 1);
        return true;
    }

    private static int Index(int width, int height, int range, bool dual) =>
        (((width * 13 + height) * 32 + range) * 2) + (dual ? 1 : 0);

    private static ushort[] Build()
    {
        var result = new ushort[13 * 13 * 32 * 2];
        for (ushort bits = 0; bits < 2048; bits++)
        {
            if (!BlockModeDecoder.TryDecodeWeightConfig(bits, out var width, out var height, out var range, out var dual) ||
                width * height * (dual ? 2 : 1) > 64)
            {
                continue;
            }

            var index = Index(width, height, range, dual);
            if (result[index] == 0)
            {
                result[index] = (ushort)(bits + 1);
            }
        }

        return result;
    }
}

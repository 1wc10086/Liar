namespace LiarUtil.Core.Wem;

internal static class WemCrc32
{
    private static readonly uint[] Table = CreateTable();

    public static uint Compute(ReadOnlySpan<byte> data)
    {
        var hash = 0U;
        foreach (var value in data)
        {
            hash = (hash << 8) ^ Table[(int)(((hash >> 24) & 0xFF) ^ value)];
        }

        return hash;
    }

    private static uint[] CreateTable()
    {
        var table = new uint[256];
        for (var index = 0; index < table.Length; index++)
        {
            var value = (uint)(index << 24);
            for (var bit = 0; bit < 8; bit++)
            {
                value = (value & 0x80000000) != 0 ? (value << 1) ^ 0x04C11DB7 : value << 1;
            }

            table[index] = value;
        }

        return table;
    }
}

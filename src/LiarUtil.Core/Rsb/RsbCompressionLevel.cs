namespace LiarUtil.Core.Rsb;

internal static class RsbCompressionLevel
{
    public const string Fastest = "Fastest";
    public const string Optimal = "Optimal";
    public const string Smallest = "Smallest";

    public static string Parse(string value) => value.ToLowerInvariant() switch
    {
        "fastest" => Fastest,
        "smallest" => Smallest,
        "optimal" => Optimal,
        _ => throw new InvalidDataException($"Unsupported compression level: {value}"),
    };

    public static string FromZlibHeader(byte value) => (value >> 6) switch
    {
        0 => Fastest,
        3 => Smallest,
        _ => Optimal,
    };

    public static int ToZlibLevel(string value) => value switch
    {
        Fastest => 1,
        Smallest => 9,
        _ => 6,
    };
}

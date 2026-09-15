namespace LiarUtil.Core.Rsb;

internal static class RsbResourceKind
{
    public const string General = "General";
    public const string Texture = "Texture";

    public static string Parse(string value) => value.ToLowerInvariant() switch
    {
        "general" => General,
        "texture" => Texture,
        _ => throw new InvalidDataException($"Unsupported resource kind: {value}"),
    };
}

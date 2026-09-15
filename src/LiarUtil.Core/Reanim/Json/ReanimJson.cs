using System.Text.Json;

namespace LiarUtil.Core.Reanim.Json;

internal static class ReanimJson
{
    public static string Serialize(ReanimFile reanim) =>
        JsonSerializer.Serialize(reanim, ReanimJsonContext.Default.ReanimFile);

    public static ReanimFile Deserialize(string json) =>
        JsonSerializer.Deserialize(json, ReanimJsonContext.Default.ReanimFile)
        ?? throw new InvalidDataException(LiarUtil.Core.Strings.ReanimJSONContentEmpty);
}

using System.Text.Json;
using LiarUtil.Core.Trail;

namespace LiarUtil.Core.Trail.Json;

internal static class TrailJson
{
    public static string Serialize(TrailFile trail) =>
        JsonSerializer.Serialize(trail, TrailJsonContext.Default.TrailFile);

    public static TrailFile Deserialize(string json) =>
        JsonSerializer.Deserialize(json, TrailJsonContext.Default.TrailFile)
        ?? throw new InvalidDataException(LiarUtil.Core.Strings.TrailJSONContentEmpty);
}

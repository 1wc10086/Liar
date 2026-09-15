using System.Text.Json;
using System.Text.Json.Nodes;
using LiarUtil.Core.Core.IO;

namespace LiarUtil.Core.Atlas.Formats;

internal sealed class ResJsonFormat : IAtlasFormat
{
    public AtlasReadResult Read(string infoPath, string inputFile, string? itemName)
    {
        var json = JsonNode.Parse(FileIO.ReadAllText(infoPath)) as JsonObject
            ?? throw new AtlasException(LiarUtil.Core.Strings.JSONRootNodeMustObject);
        var stem = XmlHelper.PathStem(inputFile);
        var found = Find(json, itemName, stem);
        var atlas = found.Atlas;
        var id = string.IsNullOrEmpty(itemName) ? atlas["id"]?.GetValue<string>() ?? "" : itemName;
        var subImages = new List<AtlasSubImage>();
        foreach (var node in found.Resources)
        {
            if (node is JsonObject resource && resource["type"]?.GetValue<string>() == "Image" &&
                resource["parent"]?.GetValue<string>() == atlas["id"]?.GetValue<string>())
            {
                subImages.Add(new AtlasSubImage(
                    resource["id"]?.GetValue<string>() ?? "",
                    resource["ax"]?.GetValue<int>() ?? 0,
                    resource["ay"]?.GetValue<int>() ?? 0,
                    resource["aw"]?.GetValue<int>() ?? 0,
                    resource["ah"]?.GetValue<int>() ?? 0));
            }
        }

        if (subImages.Count == 0)
        {
            throw new AtlasException(LiarUtil.Core.Strings.AtlasContainsNoSubImages);
        }

        return new AtlasReadResult(id, subImages);
    }

    public void Write(string infoPath, string outputFile, string? itemName, int width, int height, IReadOnlyDictionary<string, AtlasSubImage> subImages)
    {
        var json = JsonNode.Parse(FileIO.ReadAllText(infoPath)) as JsonObject
            ?? throw new AtlasException(LiarUtil.Core.Strings.JSONRootNodeMustObject);
        var stem = XmlHelper.PathStem(outputFile);
        var found = Find(json, itemName, stem);
        found.Atlas["width"] = width;
        found.Atlas["height"] = height;
        var atlasId = found.Atlas["id"]?.GetValue<string>() ?? "";
        foreach (var node in found.Resources)
        {
            if (node is not JsonObject resource || resource["type"]?.GetValue<string>() != "Image" ||
                resource["parent"]?.GetValue<string>() != atlasId)
            {
                continue;
            }

            var key = resource["id"]?.GetValue<string>() ?? "";
            if (!subImages.TryGetValue(key.ToLowerInvariant(), out var subImage))
            {
                throw new AtlasException(string.Format(LiarUtil.Core.Strings.MissingInputImage0, key));
            }

            resource["ax"] = subImage.X;
            resource["ay"] = subImage.Y;
            resource["aw"] = subImage.Width;
            resource["ah"] = subImage.Height;
        }

        FileIO.WriteAllText(infoPath, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static (JsonObject Atlas, JsonArray Resources) Find(JsonObject json, string? itemName, string stem)
    {
        if (json["groups"] is not JsonArray groups)
        {
            throw new AtlasException(LiarUtil.Core.Strings.JSONLacksGroupsArray);
        }

        foreach (var node in groups)
        {
            if (node is not JsonObject group || group["type"]?.GetValue<string>() != "simple" || group["resources"] is not JsonArray resources)
            {
                continue;
            }

            foreach (var item in resources)
            {
                if (item is not JsonObject resource || resource["type"]?.GetValue<string>() != "Image" ||
                    resource["atlas"]?.GetValue<bool>() != true)
                {
                    continue;
                }

                var matches = string.IsNullOrEmpty(itemName)
                    ? LastSegment(resource["path"]) == stem
                    : resource["id"]?.GetValue<string>() == itemName;
                if (matches)
                {
                    return (resource, resources);
                }
            }
        }

        throw new AtlasException(LiarUtil.Core.Strings.AtlasInfoNotFound);
    }

    private static string LastSegment(JsonNode? path) => path switch
    {
        JsonArray array when array.Count > 0 => XmlHelper.PathStem(array[array.Count - 1]?.GetValue<string>() ?? ""),
        JsonValue value => XmlHelper.PathStem(value.GetValue<string>()),
        _ => "",
    };
}

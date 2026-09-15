using System.Text.Json.Nodes;
using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Newton;

public static class NewtonEncoder
{
    public static byte[] EncodeFromJson(string json) => Encode(NewtonJson.ParseObject(json));

    public static byte[] Encode(JsonObject definition)
    {
        var groups = RequireArray(definition["groups"], "definition.groups");
        var writer = new BufferWriter(ByteOrder.Big);
        writer.WriteInt32(RequireInteger(definition["slot_count"], "slot_count"));
        writer.WriteInt32(groups.Count);
        foreach (var node in groups)
        {
            WriteGroup(writer, RequireObject(node, "group"));
        }

        return writer.ToArray();
    }

    private static void WriteGroup(BufferWriter writer, JsonObject group)
    {
        var type = RequireString(group["type"], "group.type");
        writer.WriteUInt8(type switch
        {
            NewtonConstants.CompositeGroupType => 1,
            NewtonConstants.SimpleGroupType => 2,
            _ => throw new NewtonException(string.Format(LiarUtil.Core.Strings.UnknownGroupType0, type)),
        });

        var subgroups = OptionalArray(group["subgroups"], "group.subgroups");
        var resources = OptionalArray(group["resources"], "group.resources");
        writer.WriteInt32(OptionalInteger(group["res"], 0, "group.res"));
        writer.WriteInt32(subgroups.Count);
        writer.WriteInt32(resources.Count);
        writer.WriteBoolean(true);
        var parent = group["parent"];
        writer.WriteBoolean(parent is not null);
        writer.WriteStringByInt32Head(RequireString(group["id"], "group.id"));
        if (parent is not null)
        {
            writer.WriteStringByInt32Head(RequireString(parent, "group.parent"));
        }

        foreach (var node in subgroups)
        {
            var subgroup = RequireObject(node, "subgroup");
            writer.WriteInt32(OptionalInteger(subgroup["res"], 0, "subgroup.res"));
            writer.WriteStringByInt32Head(RequireString(subgroup["id"], "subgroup.id"));
        }

        foreach (var node in resources)
        {
            WriteResource(writer, RequireObject(node, "resource"));
        }
    }

    private static void WriteResource(BufferWriter writer, JsonObject resource)
    {
        var type = RequireString(resource["type"], "resource.type");
        var index = Array.IndexOf(NewtonConstants.ResourceTypes, type);
        if (index < 0)
        {
            throw new NewtonException(string.Format(LiarUtil.Core.Strings.UnknownResourceType0, type));
        }

        var parent = resource["parent"];
        var hasParent = parent is not null;
        writer.WriteUInt8((byte)(index + 1));
        writer.WriteInt32(RequireInteger(resource["slot"], "resource.slot"));
        writer.WriteInt32(OptionalInteger(resource["width"], 0, "resource.width"));
        writer.WriteInt32(OptionalInteger(resource["height"], 0, "resource.height"));
        writer.WriteInt32(OptionalInteger(resource["x"], 0, "resource.x"));
        writer.WriteInt32(type != NewtonConstants.ImageResourceType || !hasParent
            ? 0x7FFFFFFF
            : OptionalInteger(resource["y"], 0, "resource.y"));
        writer.WriteInt32(OptionalInteger(resource["ax"], 0, "resource.ax"));
        writer.WriteInt32(OptionalInteger(resource["ay"], 0, "resource.ay"));
        writer.WriteInt32(OptionalInteger(resource["aw"], 0, "resource.aw"));
        writer.WriteInt32(OptionalInteger(resource["ah"], 0, "resource.ah"));
        writer.WriteInt32(OptionalInteger(resource["cols"], 1, "resource.cols"));
        writer.WriteInt32(OptionalInteger(resource["rows"], 1, "resource.rows"));
        writer.WriteBoolean(resource["atlas"]?.GetValue<bool>() ?? false);
        writer.WriteBoolean(true);
        writer.WriteBoolean(true);
        writer.WriteBoolean(hasParent);
        writer.WriteStringByInt32Head(RequireString(resource["id"], "resource.id"));
        writer.WriteStringByInt32Head(FormatPath(resource["path"]));
        if (hasParent)
        {
            writer.WriteStringByInt32Head(RequireString(parent, "resource.parent"));
        }
    }

    private static string FormatPath(JsonNode? node) => node switch
    {
        null => throw new NewtonException(LiarUtil.Core.Strings.ResourcePathCannotEmpty),
        JsonArray array => string.Join("\\", array.Select(item => RequireString(item, "resource.path"))),
        _ => RequireString(node, "resource.path"),
    };

    private static JsonObject RequireObject(JsonNode? node, string name) =>
        node as JsonObject ?? throw new NewtonException(string.Format(LiarUtil.Core.Strings.N0MustObject, name));

    private static JsonArray RequireArray(JsonNode? node, string name) =>
        node as JsonArray ?? throw new NewtonException(string.Format(LiarUtil.Core.Strings.N0MustArray, name));

    private static JsonArray OptionalArray(JsonNode? node, string name) => node is null ? new JsonArray() : RequireArray(node, name);

    private static int RequireInteger(JsonNode? node, string name) =>
        node?.GetValue<int>() ?? throw new NewtonException(string.Format(LiarUtil.Core.Strings.N0Must32BitSignedInteger, name));

    private static int OptionalInteger(JsonNode? node, int fallback, string name) => node is null ? fallback : RequireInteger(node, name);

    private static string RequireString(JsonNode? node, string name) =>
        node?.GetValue<string>() ?? throw new NewtonException(string.Format(LiarUtil.Core.Strings.N0MustString, name));
}

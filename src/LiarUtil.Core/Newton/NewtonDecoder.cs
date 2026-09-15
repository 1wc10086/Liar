using System.Text.Json.Nodes;
using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Newton;

public static class NewtonDecoder
{
    public static string DecodeToJson(ReadOnlySpan<byte> data) => NewtonJson.Serialize(Decode(data));

    public static JsonObject Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4)
        {
            throw new NewtonException(LiarUtil.Core.Strings.DataLengthInsufficient);
        }

        var reader = CreateReader(data);
        var slotCount = reader.ReadInt32();
        var groups = new JsonArray();
        var groupCount = ReadCount(reader, "group count");
        for (var i = 0; i < groupCount; i++)
        {
            groups.Add(ReadGroup(reader));
        }

        return new JsonObject
        {
            ["slot_count"] = slotCount,
            ["groups"] = groups,
        };
    }

    private static BufferReader CreateReader(ReadOnlySpan<byte> data) =>
        new(data.ToArray(), ByteOrder.Big)
        {
            ErrorFactory = static message => new NewtonException(message),
        };

    private static int ReadCount(BufferReader reader, string name)
    {
        var value = reader.ReadInt32();
        return value < 0 ? throw new NewtonException(string.Format(LiarUtil.Core.Strings.N0Invalid, name)) : value;
    }

    private static bool ReadStrictBoolean(BufferReader reader)
    {
        var value = reader.ReadUInt8();
        if (value > 1)
        {
            throw new NewtonException(LiarUtil.Core.Strings.BooleanIntegerInvalid);
        }

        return value == 1;
    }

    private static string ReadString(BufferReader reader)
    {
        var length = reader.ReadInt32();
        if (length < 0)
        {
            throw new NewtonException(LiarUtil.Core.Strings.StringLengthInvalid);
        }

        reader.RequireLength((ulong)length);
        return TextCodec.Decode(reader.ReadSpan(length), TextFormat.Utf8);
    }

    private static JsonObject ReadGroup(BufferReader reader)
    {
        var groupType = reader.ReadUInt8();
        if (groupType is not (1 or 2))
        {
            throw new NewtonException(string.Format(LiarUtil.Core.Strings.UnknownGroupTypeOrdinal0, groupType));
        }

        var res = reader.ReadInt32();
        var subgroupCount = ReadCount(reader, "subgroup count");
        var resourceCount = ReadCount(reader, "resource count");
        if (!ReadStrictBoolean(reader))
        {
            throw new NewtonException(LiarUtil.Core.Strings.GroupFixedMarkerInvalid);
        }

        var hasParent = ReadStrictBoolean(reader);
        var id = ReadString(reader);
        var parent = hasParent ? ReadString(reader) : null;
        var subgroups = new JsonArray();
        for (var i = 0; i < subgroupCount; i++)
        {
            var subgroupRes = reader.ReadInt32();
            var subgroupId = ReadString(reader);
            var subgroup = new JsonObject { ["id"] = subgroupId };
            if (subgroupRes != 0)
            {
                subgroup["res"] = subgroupRes;
            }

            subgroups.Add(subgroup);
        }

        var resources = new JsonArray();
        for (var i = 0; i < resourceCount; i++)
        {
            resources.Add(ReadResource(reader));
        }

        var group = new JsonObject
        {
            ["id"] = id,
            ["type"] = groupType == 1 ? NewtonConstants.CompositeGroupType : NewtonConstants.SimpleGroupType,
        };
        if (groupType == 1)
        {
            if (resourceCount != 0)
            {
                throw new NewtonException(LiarUtil.Core.Strings.CompositeGroupContainsResources);
            }

            group["subgroups"] = subgroups;
            return group;
        }

        if (subgroupCount != 0)
        {
            throw new NewtonException(LiarUtil.Core.Strings.SimpleGroupContainsSubgroups);
        }

        if (res != 0)
        {
            group["res"] = res;
        }

        if (parent is not null)
        {
            group["parent"] = parent;
        }

        group["resources"] = resources;
        return group;
    }

    private static JsonObject ReadResource(BufferReader reader)
    {
        var resourceType = reader.ReadUInt8();
        if (resourceType is < 1 or > 7)
        {
            throw new NewtonException(string.Format(LiarUtil.Core.Strings.UnknownResourceTypeOrdinal0, resourceType));
        }

        var type = NewtonConstants.ResourceTypes[resourceType - 1];
        var slot = reader.ReadInt32();
        var width = reader.ReadInt32();
        var height = reader.ReadInt32();
        var x = reader.ReadInt32();
        var y = reader.ReadInt32();
        var ax = reader.ReadInt32();
        var ay = reader.ReadInt32();
        var aw = reader.ReadInt32();
        var ah = reader.ReadInt32();
        var cols = reader.ReadInt32();
        var rows = reader.ReadInt32();
        var atlas = ReadStrictBoolean(reader);
        if (!ReadStrictBoolean(reader) || !ReadStrictBoolean(reader))
        {
            throw new NewtonException(LiarUtil.Core.Strings.ResourceFixedMarkerInvalid);
        }

        var hasParent = ReadStrictBoolean(reader);
        var id = ReadString(reader);
        var path = ReadString(reader);
        var parent = hasParent ? ReadString(reader) : null;

        var resource = new JsonObject
        {
            ["slot"] = slot,
            ["id"] = id,
            ["path"] = path,
            ["type"] = type,
        };
        if (type != NewtonConstants.ImageResourceType)
        {
            return resource;
        }

        if (hasParent)
        {
            resource["parent"] = parent;
            resource["ax"] = ax;
            resource["ay"] = ay;
            resource["aw"] = aw;
            resource["ah"] = ah;
            resource["x"] = x;
            resource["y"] = y;
            resource["rows"] = rows;
            resource["cols"] = cols;
            return resource;
        }

        resource["atlas"] = atlas;
        resource["width"] = width;
        resource["height"] = height;
        return resource;
    }
}

using System.Text;
using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Rsb.Resources;

internal static class RsbResourceDefinitionCodec
{
    private const int HeaderSize = 0x14;
    private const int EntrySize = 0x1C;
    private const int TextureSize = 24;
    private const int PropertySize = 12;

    public static List<RsbResourceDefinition> Parse(byte[] data, bool bigEndian)
    {
        var reader = new BufferReader(data, bigEndian);
        if (reader.ReadInt32() != RsbConstants.ResourceDefinitionMagic || reader.ReadInt32() != 1)
        {
            throw new InvalidDataException("Invalid RSB resource definition header");
        }

        var part1Begin = reader.ReadInt32();
        var part2Begin = reader.ReadInt32();
        var part3Begin = reader.ReadInt32();
        if (part1Begin < HeaderSize || part2Begin < part1Begin || part3Begin < part2Begin || part3Begin >= data.Length)
        {
            throw new InvalidDataException("Invalid RSB definition section offsets");
        }

        var definitions = new List<RsbResourceDefinition>();
        reader.Position = part1Begin;
        while (reader.Position < part2Begin)
        {
            var definition = new RsbResourceDefinition { Identifier = ReadString(data, part3Begin, reader.ReadInt32()) };
            var groupCount = reader.ReadInt32();
            _ = reader.ReadInt32();
            RsbValidation.Range(part2Begin, reader.Position, (long)groupCount * 16, "definition groups");
            for (var i = 0; i < groupCount; i++)
            {
                var group = new RsbResourceDefinitionGroup
                {
                    Resolution = reader.ReadInt32(),
                    Locale = RsbText.ReadFourCharacterCode(reader),
                    Identifier = ReadString(data, part3Begin, reader.ReadInt32()),
                };
                var resourceCount = reader.ReadInt32();
                RsbValidation.Range(part2Begin, reader.Position, (long)resourceCount * 4, "definition resource offsets");
                var offsets = new int[resourceCount];
                for (var j = 0; j < resourceCount; j++)
                {
                    offsets[j] = reader.ReadInt32();
                }

                var next = reader.Position;
                foreach (var offset in offsets)
                {
                    group.Resources.Add(ReadEntry(data, reader, part2Begin, part3Begin, offset));
                }

                reader.Position = next;
                definition.Groups.Add(group);
            }

            definitions.Add(definition);
        }

        return definitions;
    }

    public static byte[] Encode(IReadOnlyList<RsbResourceDefinition> definitions, bool bigEndian)
    {
        var part1 = new BufferWriter(bigEndian);
        var part2 = new BufferWriter(bigEndian);
        var part3 = new List<byte> { 0 };
        var stringPool = new Dictionary<string, int> { [""] = 0 };

        int Intern(string value)
        {
            if (stringPool.TryGetValue(value, out var offset))
            {
                return offset;
            }

            offset = part3.Count;
            stringPool[value] = offset;
            part3.AddRange(Encoding.UTF8.GetBytes(value));
            part3.Add(0);
            return offset;
        }

        foreach (var definition in definitions)
        {
            part1.WriteInt32(Intern(definition.Identifier));
            part1.WriteInt32(definition.Groups.Count);
            part1.WriteInt32(0x10);
            foreach (var group in definition.Groups)
            {
                part1.WriteInt32(group.Resolution);
                RsbText.WriteFourCharacterCode(part1, group.Locale);
                part1.WriteInt32(Intern(group.Identifier));
                part1.WriteInt32(group.Resources.Count);
                foreach (var resource in group.Resources)
                {
                    WriteEntry(part1, part2, resource, Intern);
                }
            }
        }

        var output = new BufferWriter(bigEndian);
        output.WriteInt32(RsbConstants.ResourceDefinitionMagic);
        output.WriteInt32(1);
        output.WriteInt32(HeaderSize);
        output.WriteInt32(HeaderSize + part1.Position);
        output.WriteInt32(HeaderSize + part1.Position + part2.Position);
        output.WriteBytes(part1.ToArray());
        output.WriteBytes(part2.ToArray());
        var result = output.ToArray();
        return [.. result, .. part3];
    }

    private static RsbResourceDefinitionEntry ReadEntry(byte[] data, BufferReader reader, int part2Begin, int part3Begin, int offset)
    {
        RsbValidation.Range(part3Begin - part2Begin, offset, EntrySize, "definition resource");
        reader.Position = checked(part2Begin + offset);
        _ = reader.ReadInt32();
        var entry = new RsbResourceDefinitionEntry { Type = reader.ReadUInt16() };
        reader.Position += 2;
        var textureEnd = reader.ReadInt32();
        var textureBegin = reader.ReadInt32();
        entry.Identifier = ReadString(data, part3Begin, reader.ReadInt32());
        entry.Path = ReadString(data, part3Begin, reader.ReadInt32());
        var propertyCount = reader.ReadInt32();
        if (textureBegin != 0 && textureEnd != 0)
        {
            RsbValidation.Range(part3Begin - part2Begin, textureBegin, (long)textureEnd - textureBegin, "definition texture");
            if (textureEnd - textureBegin != TextureSize || reader.Position != part2Begin + textureBegin)
            {
                throw new InvalidDataException("Invalid RSB definition texture layout");
            }

            entry.Texture = new RsbResourceDefinitionTexture
            {
                Type = reader.ReadUInt16(),
                Flags = reader.ReadUInt16(),
                X = reader.ReadUInt16(),
                Y = reader.ReadUInt16(),
                AnchorX = reader.ReadUInt16(),
                AnchorY = reader.ReadUInt16(),
                AnchorWidth = reader.ReadUInt16(),
                AnchorHeight = reader.ReadUInt16(),
                Rows = reader.ReadUInt16(),
                Cols = reader.ReadUInt16(),
                Parent = ReadString(data, part3Begin, reader.ReadInt32()),
            };
        }

        RsbValidation.Range(part3Begin, reader.Position, (long)propertyCount * PropertySize, "definition properties");
        if (propertyCount > 0)
        {
            entry.Properties = new Dictionary<string, string>(propertyCount);
            for (var i = 0; i < propertyCount; i++)
            {
                var key = ReadString(data, part3Begin, reader.ReadInt32());
                _ = reader.ReadInt32();
                entry.Properties[key] = ReadString(data, part3Begin, reader.ReadInt32());
            }
        }

        return entry;
    }

    private static void WriteEntry(BufferWriter part1, BufferWriter part2, RsbResourceDefinitionEntry resource, Func<string, int> intern)
    {
        var properties = resource.Properties ?? [];
        part1.WriteInt32(part2.Position);
        part2.WriteInt32(0);
        part2.WriteUInt16(resource.Type);
        part2.WriteUInt16(EntrySize);
        var textureEndPosition = part2.Position;
        part2.WriteInt32(0);
        part2.WriteInt32(0);
        part2.WriteInt32(intern(resource.Identifier));
        part2.WriteInt32(intern(resource.Path));
        part2.WriteInt32(properties.Count);

        if (resource.Type == 0)
        {
            var textureBegin = part2.Position;
            var texture = resource.Texture;
            if (texture is null)
            {
                for (var i = 0; i < 8; i++)
                {
                    part2.WriteUInt16(0);
                }

                part2.WriteUInt16(1);
                part2.WriteUInt16(1);
                part2.WriteInt32(intern(""));
            }
            else
            {
                part2.WriteUInt16(texture.Type);
                part2.WriteUInt16(texture.Flags);
                part2.WriteUInt16(texture.X);
                part2.WriteUInt16(texture.Y);
                part2.WriteUInt16(texture.AnchorX);
                part2.WriteUInt16(texture.AnchorY);
                part2.WriteUInt16(texture.AnchorWidth);
                part2.WriteUInt16(texture.AnchorHeight);
                part2.WriteUInt16(texture.Rows);
                part2.WriteUInt16(texture.Cols);
                part2.WriteInt32(intern(texture.Parent));
            }

            var textureEnd = part2.Position;
            part2.Position = textureEndPosition;
            part2.WriteInt32(textureEnd);
            part2.WriteInt32(textureBegin);
            part2.Position = textureEnd;
        }

        foreach (var (key, value) in properties)
        {
            part2.WriteInt32(intern(key));
            part2.WriteInt32(0);
            part2.WriteInt32(intern(value));
        }
    }

    private static string ReadString(byte[] data, int part3Begin, int offset)
    {
        if (offset < 0 || offset >= data.Length - part3Begin)
        {
            throw new InvalidDataException("Invalid RSB definition string offset");
        }

        var start = checked(part3Begin + offset);
        var end = start;
        while (end < data.Length && data[end] != 0)
        {
            end++;
        }

        if (end == data.Length)
        {
            throw new InvalidDataException("Unterminated RSB definition string");
        }

        return Encoding.UTF8.GetString(data, start, end - start);
    }
}

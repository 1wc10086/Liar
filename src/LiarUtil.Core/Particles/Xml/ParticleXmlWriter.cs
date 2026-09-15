using System.Globalization;
using System.Text;

namespace LiarUtil.Core.Particles.Xml;

internal static class ParticleXmlWriter
{
    public static string Write(ParticleFile file)
    {
        var builder = new StringBuilder();
        foreach (var emitter in file.Emitters)
        {
            WriteEmitter(builder, emitter);
        }

        return builder.ToString();
    }

    private static void WriteEmitter(StringBuilder builder, ParticleEmitter emitter)
    {
        builder.Append("<Emitter>\n");
        AppendElement(builder, "Name", emitter.Name);
        AppendElement(builder, "Image", ParticleImageHelper.ResolveName(emitter.Image));
        AppendElement(builder, "ImageResource", emitter.Image?.Resource);
        AppendIntElement(builder, "ImageCol", emitter.Image?.Columns);
        AppendIntElement(builder, "ImageRow", emitter.Image?.Rows);
        AppendIntElement(builder, "ImageFrames", emitter.Image?.Frames);
        AppendIntElement(builder, "Animated", emitter.Image?.Animated);

        if (emitter.EmitterType is { } emitterType)
        {
            builder.Append("  <EmitterType>");
            builder.Append(FormatEmitterType((int)emitterType));
            builder.Append("</EmitterType>\n");
        }

        if (emitter.Flags is { } flags)
        {
            for (var index = 0; index < ParticleNames.Flags.Length; index++)
            {
                if ((flags & (ParticleFlags)(1 << index)) != 0)
                {
                    AppendElement(builder, ParticleNames.Flags[index], "1");
                }
            }
        }

        AppendElement(builder, "OnDuration", emitter.OnDuration);
        foreach (var track in ParticleXmlTracks.All)
        {
            var nodes = track.Get(emitter);
            if (nodes is { Count: > 0 })
            {
                builder.Append("  <").Append(track.Element).Append('>');
                builder.Append(ParticleTrackText.Format(nodes));
                builder.Append("</").Append(track.Element).Append(">\n");
            }
        }

        WriteFields(builder, emitter.Fields, "Field");
        WriteFields(builder, emitter.SystemFields, "SystemField");
        builder.Append("</Emitter>\n");
    }

    private static void WriteFields(StringBuilder builder, List<ParticleField>? fields, string name)
    {
        if (fields is null)
        {
            return;
        }

        foreach (var field in fields)
        {
            builder.Append("  <").Append(name).Append(">\n");
            if (field.Type is { } type)
            {
                builder.Append("    <FieldType>").Append(FormatFieldType((int)type)).Append("</FieldType>\n");
            }

            if (field.X is { Count: > 0 })
            {
                builder.Append("    <X>").Append(ParticleTrackText.Format(field.X)).Append("</X>\n");
            }

            if (field.Y is { Count: > 0 })
            {
                builder.Append("    <Y>").Append(ParticleTrackText.Format(field.Y)).Append("</Y>\n");
            }

            builder.Append("  </").Append(name).Append(">\n");
        }
    }

    private static string FormatEmitterType(int index) =>
        index >= 0 && index < ParticleNames.EmitterTypes.Length
            ? ParticleNames.EmitterTypes[index]
            : $"Emitter({index})";

    private static string FormatFieldType(int index) =>
        index >= 0 && index < ParticleNames.FieldTypes.Length
            ? ParticleNames.FieldTypes[index]
            : $"Field({index})";

    private static void AppendElement(StringBuilder builder, string name, string? value)
    {
        if (value is null)
        {
            return;
        }

        builder.Append("  <").Append(name).Append('>').Append(value).Append("</").Append(name).Append(">\n");
    }

    private static void AppendIntElement(StringBuilder builder, string name, int? value)
    {
        if (value is { } number)
        {
            AppendElement(builder, name, number.ToString(CultureInfo.InvariantCulture));
        }
    }
}

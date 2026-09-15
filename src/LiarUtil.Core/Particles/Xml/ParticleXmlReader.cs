using System.Globalization;
using System.Xml.Linq;
using LiarUtil.Core.Core.Xml;

namespace LiarUtil.Core.Particles.Xml;

internal static class ParticleXmlReader
{
    public static ParticleFile Read(string text)
    {
        var wrapped = "<root>" + text.Replace("&", "&amp;", StringComparison.Ordinal) + "</root>";
        var document = XDocument.Parse(wrapped, LoadOptions.None);
        var root = document.Root ?? throw new InvalidDataException(LiarUtil.Core.Strings.ParticleXMLContentInvalid);
        var file = new ParticleFile();
        foreach (var element in root.Elements("Emitter"))
        {
            file.Emitters.Add(ReadEmitter(element));
        }

        return file;
    }

    private static ParticleEmitter ReadEmitter(XElement element)
    {
        var emitter = new ParticleEmitter();
        var fields = new List<ParticleField>();
        var systemFields = new List<ParticleField>();
        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            var value = child.Value;
            if (ParticleXmlTracks.ByElement.TryGetValue(name, out var track))
            {
                track.Set(emitter, ParticleTrackText.Parse(value));
                continue;
            }

            switch (name)
            {
                case "Name":
                    emitter.Name = XmlText.EmptyToNull(value);
                    break;
                case "Image":
                    SetImageName(emitter, value);
                    break;
                case "ImageResource":
                    SetImageResource(emitter, value);
                    break;
                case "ImageCol":
                    if (XmlNumbers.TryInt(value, out var columns))
                    {
                        emitter.Image ??= new ParticleImage();
                        emitter.Image.Columns = columns;
                    }

                    break;
                case "ImageRow":
                    if (XmlNumbers.TryInt(value, out var rows))
                    {
                        emitter.Image ??= new ParticleImage();
                        emitter.Image.Rows = rows;
                    }

                    break;
                case "ImageFrames":
                    if (XmlNumbers.TryInt(value, out var frames))
                    {
                        emitter.Image ??= new ParticleImage();
                        emitter.Image.Frames = frames;
                    }

                    break;
                case "Animated":
                    if (XmlNumbers.TryInt(value, out var animated))
                    {
                        emitter.Image ??= new ParticleImage();
                        emitter.Image.Animated = animated;
                    }

                    break;
                case "EmitterType":
                    emitter.EmitterType = ParseEmitterType(value);
                    break;
                case "OnDuration":
                    emitter.OnDuration = XmlText.EmptyToNull(value);
                    break;
                case "Field":
                    fields.Add(ReadField(child));
                    break;
                case "SystemField":
                    systemFields.Add(ReadField(child));
                    break;
                default:
                    var flagIndex = Array.IndexOf(ParticleNames.Flags, name);
                    if (flagIndex >= 0 && value == "1")
                    {
                        emitter.Flags = (emitter.Flags ?? ParticleFlags.None) | (ParticleFlags)(1 << flagIndex);
                    }

                    break;
            }
        }

        if (fields.Count > 0)
        {
            emitter.Fields = fields;
        }

        if (systemFields.Count > 0)
        {
            emitter.SystemFields = systemFields;
        }

        return emitter;
    }

    private static ParticleField ReadField(XElement element)
    {
        var field = new ParticleField();
        foreach (var child in element.Elements())
        {
            switch (child.Name.LocalName)
            {
                case "FieldType":
                    field.Type = ParseFieldType(child.Value);
                    break;
                case "X":
                    field.X = ParticleTrackText.Parse(child.Value);
                    break;
                case "Y":
                    field.Y = ParticleTrackText.Parse(child.Value);
                    break;
            }
        }

        return field;
    }

    private static void SetImageName(ParticleEmitter emitter, string value)
    {
        var name = XmlText.EmptyToNull(value);
        if (name is null)
        {
            return;
        }

        emitter.Image ??= new ParticleImage();
        emitter.Image.Name = name;
    }

    private static void SetImageResource(ParticleEmitter emitter, string value)
    {
        var resource = XmlText.EmptyToNull(value);
        if (resource is null)
        {
            return;
        }

        emitter.Image ??= new ParticleImage();
        emitter.Image.Resource = resource;
    }

    private static ParticleEmitterType ParseEmitterType(string value)
    {
        var index = Array.IndexOf(ParticleNames.EmitterTypes, value);
        if (index >= 0)
        {
            return (ParticleEmitterType)index;
        }

        if (TryWrapped(value, "Emitter(", out var number))
        {
            return (ParticleEmitterType)number;
        }

        throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.InvalidEmitterType0, value));
    }

    private static ParticleFieldType ParseFieldType(string value)
    {
        var index = Array.IndexOf(ParticleNames.FieldTypes, value);
        if (index >= 0)
        {
            return (ParticleFieldType)index;
        }

        if (TryWrapped(value, "Field(", out var number))
        {
            return (ParticleFieldType)number;
        }

        throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.InvalidFieldType0, value));
    }

    private static bool TryWrapped(string value, string prefix, out int number)
    {
        number = 0;
        if (!value.StartsWith(prefix, StringComparison.Ordinal) || !value.EndsWith(')'))
        {
            return false;
        }

        var body = value[prefix.Length..^1];
        return int.TryParse(body, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
    }
}

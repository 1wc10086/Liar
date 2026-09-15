using System.Globalization;
using System.Xml.Linq;
using LiarUtil.Core.Core.Xml;
using LiarUtil.Core.PopCap;

namespace LiarUtil.Core.Reanim;

internal static class ReanimXml
{
    public static string Serialize(ReanimFile reanim)
    {
        var elements = new List<XElement>();
        if (reanim.DoScale is { } doScale)
        {
            elements.Add(new XElement("doScale", doScale.ToString(CultureInfo.InvariantCulture)));
        }
        elements.Add(new XElement("fps", reanim.Fps.ToString(CultureInfo.InvariantCulture)));
        foreach (var track in reanim.Tracks)
        {
            elements.Add(WriteTrack(track));
        }
        return XmlFragment.Render(elements);
    }

    public static ReanimFile Deserialize(string xml)
    {
        var root = XmlFragment.Parse(xml);
        var reanim = new ReanimFile { Fps = 12f };
        foreach (var element in root.Elements())
        {
            switch (element.Name.LocalName)
            {
                case "doScale":
                    reanim.DoScale = sbyte.Parse(element.Value, CultureInfo.InvariantCulture);
                    break;
                case "fps":
                    reanim.Fps = XmlNumbers.ParseFloat(element.Value);
                    break;
                case "track":
                    reanim.Tracks.Add(ReadTrack(element));
                    break;
            }
        }
        return reanim;
    }

    private static XElement WriteTrack(ReanimTrack track)
    {
        var element = new XElement("track");
        if (!string.IsNullOrEmpty(track.Name))
        {
            element.Add(new XElement("name", track.Name));
        }
        foreach (var transform in track.Transforms)
        {
            element.Add(WriteTransform(transform));
        }
        return element;
    }

    private static XElement WriteTransform(ReanimTransform transform)
    {
        var element = new XElement("t");
        AppendFloat(element, "x", transform.X);
        AppendFloat(element, "y", transform.Y);
        AppendFloat(element, "kx", transform.SkewX);
        AppendFloat(element, "ky", transform.SkewY);
        AppendFloat(element, "sx", transform.ScaleX);
        AppendFloat(element, "sy", transform.ScaleY);
        AppendFloat(element, "f", transform.Frame);
        AppendFloat(element, "a", transform.Alpha);
        if (transform.Image is not null)
        {
            element.Add(new XElement("i", ImageReference.ResolveName(transform.Image)));
        }
        AppendString(element, "resource", transform.ImageResource);
        if (transform.Image2 is not null)
        {
            element.Add(new XElement("i2", ImageReference.ResolveName(transform.Image2)));
        }
        AppendString(element, "resource2", transform.Image2Resource);
        AppendString(element, "font", transform.Font);
        AppendString(element, "text", transform.Text);
        return element;
    }

    private static ReanimTrack ReadTrack(XElement element)
    {
        var track = new ReanimTrack();
        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName == "name")
            {
                track.Name = child.Value;
                continue;
            }
            if (child.Name.LocalName != "t")
            {
                continue;
            }
            var transform = new ReanimTransform();
            foreach (var node in child.Elements())
            {
                switch (node.Name.LocalName)
                {
                    case "x":
                        transform.X = XmlNumbers.ParseFloat(node.Value);
                        break;
                    case "y":
                        transform.Y = XmlNumbers.ParseFloat(node.Value);
                        break;
                    case "kx":
                        transform.SkewX = XmlNumbers.ParseFloat(node.Value);
                        break;
                    case "ky":
                        transform.SkewY = XmlNumbers.ParseFloat(node.Value);
                        break;
                    case "sx":
                        transform.ScaleX = XmlNumbers.ParseFloat(node.Value);
                        break;
                    case "sy":
                        transform.ScaleY = XmlNumbers.ParseFloat(node.Value);
                        break;
                    case "f":
                        transform.Frame = XmlNumbers.ParseFloat(node.Value);
                        break;
                    case "a":
                        transform.Alpha = XmlNumbers.ParseFloat(node.Value);
                        break;
                    case "i":
                        transform.Image = ImageReference.FromName(node.Value);
                        break;
                    case "resource":
                        transform.ImageResource = node.Value;
                        break;
                    case "i2":
                        transform.Image2 = ImageReference.FromName(node.Value);
                        break;
                    case "resource2":
                        transform.Image2Resource = node.Value;
                        break;
                    case "font":
                        transform.Font = node.Value;
                        break;
                    case "text":
                        transform.Text = node.Value;
                        break;
                }
            }
            track.Transforms.Add(transform);
        }
        return track;
    }

    private static void AppendFloat(XElement element, string name, float? value)
    {
        if (value is { } number)
        {
            element.Add(new XElement(name, number.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static void AppendString(XElement element, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            element.Add(new XElement(name, value));
        }
    }
}

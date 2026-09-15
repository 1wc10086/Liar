using System.Globalization;
using System.Text;
using System.Xml.Linq;
using LiarUtil.Core.Core.Xml;
using LiarUtil.Core.PopCap;

namespace LiarUtil.Core.Trail;

internal static class TrailXml
{
    public static string Serialize(TrailFile trail)
    {
        var elements = new List<XElement>();
        if (trail.MaxPoints is { } maxPoints)
        {
            elements.Add(new XElement("MaxPoints", maxPoints.ToString(CultureInfo.InvariantCulture)));
        }
        if (trail.MinPointDistance is { } minPointDistance)
        {
            elements.Add(new XElement("MinPointDistance", XmlNumbers.FormatTrimmed(minPointDistance)));
        }
        if (trail.Loops)
        {
            elements.Add(new XElement("Loops", "1"));
        }
        var image = ImageReference.ResolveName(trail.Image);
        if (!string.IsNullOrEmpty(image))
        {
            elements.Add(new XElement("Image", image));
        }
        if (!string.IsNullOrEmpty(trail.ImageResource))
        {
            elements.Add(new XElement("ImageResource", trail.ImageResource));
        }

        AddTrack(elements, "WidthOverLength", trail.Tracks.WidthOverLength);
        AddTrack(elements, "WidthOverTime", trail.Tracks.WidthOverTime);
        AddTrack(elements, "AlphaOverLength", trail.Tracks.AlphaOverLength);
        AddTrack(elements, "AlphaOverTime", trail.Tracks.AlphaOverTime);
        AddTrack(elements, "TrailDuration", trail.Tracks.TrailDuration);
        return XmlFragment.Render(elements);
    }

    public static TrailFile Deserialize(string xml)
    {
        var root = XmlFragment.Parse(xml);
        var trail = new TrailFile();
        foreach (var element in root.Elements())
        {
            switch (element.Name.LocalName)
            {
                case "MaxPoints":
                    trail.MaxPoints = int.Parse(element.Value, CultureInfo.InvariantCulture);
                    break;
                case "MinPointDistance":
                    trail.MinPointDistance = float.Parse(element.Value, CultureInfo.InvariantCulture);
                    break;
                case "Loops":
                    trail.Loops = XmlNumbers.ParseFlag(element.Value);
                    break;
                case "Image":
                    trail.Image = ImageReference.FromValue(element.Value, null);
                    break;
                case "ImageResource":
                    trail.ImageResource = XmlText.EmptyToNull(element.Value);
                    break;
                case "WidthOverLength":
                    trail.Tracks.WidthOverLength = ParseTrack(element);
                    break;
                case "WidthOverTime":
                    trail.Tracks.WidthOverTime = ParseTrack(element);
                    break;
                case "AlphaOverLength":
                    trail.Tracks.AlphaOverLength = ParseTrack(element);
                    break;
                case "AlphaOverTime":
                    trail.Tracks.AlphaOverTime = ParseTrack(element);
                    break;
                case "TrailDuration":
                    trail.Tracks.TrailDuration = ParseTrack(element);
                    break;
            }
        }
        return trail;
    }

    private static void AddTrack(List<XElement> elements, string name, List<TrailTrackNode>? nodes)
    {
        if (nodes is null || nodes.Count == 0)
        {
            return;
        }
        elements.Add(new XElement(name, string.Join(' ', nodes.Select(Format))));
    }

    private static string Format(TrailTrackNode node)
    {
        var builder = new StringBuilder();
        var distribution = node.Distribution;
        var low = node.Low;
        var high = node.High;
        if (low == high)
        {
            if (distribution == PopCurve.Constant)
            {
                builder.Append('[').Append(XmlNumbers.FormatTrimmed(low)).Append(']');
            }
            else if (distribution == PopCurve.Linear)
            {
                builder.Append(XmlNumbers.FormatTrimmed(low));
            }
            else
            {
                builder.Append('[').Append(XmlNumbers.FormatTrimmed(low)).Append(' ')
                    .Append(PopCurves.Format(distribution)).Append(' ')
                    .Append(XmlNumbers.FormatTrimmed(high)).Append(']');
            }
        }
        else
        {
            builder.Append('[').Append(XmlNumbers.FormatTrimmed(low));
            if (distribution != PopCurve.Linear)
            {
                builder.Append(' ').Append(PopCurves.Format(distribution));
            }
            builder.Append(' ').Append(XmlNumbers.FormatTrimmed(high)).Append(']');
        }

        if (node.Time != 0 && node.Time != 1)
        {
            builder.Append(',').Append(XmlNumbers.FormatTrimmed(node.Time * 100));
        }
        var curve = node.Curve;
        if (curve != PopCurve.Linear)
        {
            builder.Append(' ').Append(PopCurves.Format(curve));
        }
        return builder.ToString();
    }

    private static List<TrailTrackNode> ParseTrack(XElement element)
    {
        var text = element.Value;
        var nodes = new List<TrailTrackNode>();
        var index = 0;
        while (index < text.Length)
        {
            var node = new TrailTrackNode();
            var next = text[index];
            if (next == '[')
            {
                index++;
                var end = text.IndexOfAny([' ', ']'], index);
                node.Low = XmlNumbers.ParseFloat(text[index..end]);
                if (text[end] == ']')
                {
                    node.High = node.Low;
                    node.Distribution = PopCurve.Constant;
                    index = end + 1;
                }
                else
                {
                    index = end + 1;
                    if (char.IsAsciiLetter(text[index]))
                    {
                        end = text.IndexOf(' ', index);
                        node.Distribution = PopCurves.Parse(text[index..end]);
                        index = end + 1;
                    }
                    else
                    {
                        node.Distribution = PopCurve.Linear;
                    }
                    end = text.IndexOf(']', index);
                    node.High = XmlNumbers.ParseFloat(text[index..end]);
                    index = end + 1;
                }
            }
            else if (next is '.' or '-' || char.IsAsciiDigit(next))
            {
                var end = index;
                while (end < text.Length && text[end] is not (' ' or ','))
                {
                    end++;
                }
                node.Low = XmlNumbers.ParseFloat(text[index..end]);
                node.High = node.Low;
                node.Distribution = PopCurve.Linear;
                index = end;
            }
            else
            {
                node.Low = 0;
                node.High = 0;
                node.Distribution = PopCurve.Linear;
            }

            if (index >= text.Length)
            {
                node.Time = -10000;
                node.Curve = PopCurve.Linear;
                nodes.Add(node);
                break;
            }

            if (text[index] == ',')
            {
                index++;
                var end = text.IndexOf(' ', index);
                if (end < 0)
                {
                    end = text.Length;
                }
                node.Time = XmlNumbers.ParseFloat(text[index..end]);
                index = end;
            }
            else
            {
                node.Time = -10000;
            }

            index++;
            if (index >= text.Length)
            {
                node.Curve = PopCurve.Linear;
                nodes.Add(node);
                break;
            }

            if (!char.IsAsciiLetter(text[index]))
            {
                node.Curve = PopCurve.Linear;
            }
            else
            {
                var end = text.IndexOf(' ', index);
                if (end < 0)
                {
                    end = text.Length;
                }
                node.Curve = PopCurves.Parse(text[index..end]);
                index = end + 1;
            }
            nodes.Add(node);
        }

        NormalizeTimes(nodes);
        return nodes;
    }

    private static void NormalizeTimes(List<TrailTrackNode> nodes)
    {
        if (nodes.Count == 0)
        {
            return;
        }
        if (nodes[0].Time < -1000)
        {
            nodes[0].Time = 0;
        }
        if (nodes.Count != 1 && nodes[^1].Time < -1000)
        {
            nodes[^1].Time = 100;
        }

        var delta = 0f;
        var last = 0f;
        for (var index = 0; index < nodes.Count; index++)
        {
            if (nodes[index].Time >= -1000)
            {
                last = nodes[index].Time;
                if (index < nodes.Count - 1)
                {
                    var next = index + 1;
                    while (nodes[next].Time < -1000)
                    {
                        next++;
                    }
                    delta = (nodes[next].Time - nodes[index].Time) / (next - index);
                }
            }
            else
            {
                last += delta;
                nodes[index].Time = last;
            }
            nodes[index].Time /= 100;
        }
    }
}

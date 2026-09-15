using System.Globalization;
using System.Text;

using LiarUtil.Core.Core.Xml;
using LiarUtil.Core.PopCap;

namespace LiarUtil.Core.Particles.Xml;

internal static class ParticleTrackText
{
    public static string Format(List<ParticleTrackNode>? track)
    {
        if (track is null || track.Count == 0)
        {
            return "";
        }

        var builder = new StringBuilder();
        for (var i = 0; i < track.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }

            var node = track[i];
            var distribution = node.Distribution;
            if (node.Low == node.High)
            {
                if (distribution == PopCurve.Constant)
                {
                    builder.Append('[').Append(XmlNumbers.FormatTrimmed(node.Low)).Append(']');
                }
                else if (distribution == PopCurve.Linear)
                {
                    builder.Append(XmlNumbers.FormatTrimmed(node.Low));
                }
                else
                {
                    builder.Append('[').Append(XmlNumbers.FormatTrimmed(node.Low)).Append(' ')
                        .Append(FormatCurve(distribution)).Append(' ').Append(XmlNumbers.FormatTrimmed(node.High)).Append(']');
                }
            }
            else
            {
                builder.Append('[').Append(XmlNumbers.FormatTrimmed(node.Low));
                if (distribution != PopCurve.Linear)
                {
                    builder.Append(' ').Append(FormatCurve(distribution));
                }

                builder.Append(' ').Append(XmlNumbers.FormatTrimmed(node.High)).Append(']');
            }

            if (node.Time != 0 && node.Time != 1)
            {
                builder.Append(',').Append(XmlNumbers.Format(node.Time * 100));
            }

            if (node.Curve != PopCurve.Linear)
            {
                builder.Append(' ').Append(FormatCurve(node.Curve));
            }
        }

        return builder.ToString();
    }

    public static List<ParticleTrackNode>? Parse(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var nodes = new List<ParticleTrackNode>();
        var length = text.Length;
        var i = 0;
        while (i < length)
        {
            var node = new ParticleTrackNode();
            var next = text[i];
            if (next == '[')
            {
                i++;
                var j = i;
                while (true)
                {
                    j++;
                    var current = text[j];
                    if (current == ' ' || current == ']')
                    {
                        break;
                    }
                }

                var value = ParseFloat(text.AsSpan(i, j - i));
                node.Low = value;
                if (text[j] == ']')
                {
                    node.High = value;
                    node.Distribution = PopCurve.Constant;
                    i = j + 1;
                }
                else
                {
                    i = ++j;
                    var current = text[i];
                    if (current is >= 'A' and <= 'Z')
                    {
                        while (true)
                        {
                            j++;
                            if (text[j] == ' ')
                            {
                                break;
                            }
                        }

                        node.Distribution = ParseCurve(text[i..j]);
                        i = ++j;
                    }
                    else
                    {
                        node.Distribution = PopCurve.Linear;
                    }

                    while (true)
                    {
                        j++;
                        if (text[j] == ']')
                        {
                            break;
                        }
                    }

                    node.High = ParseFloat(text.AsSpan(i, j - i));
                    i = ++j;
                }
            }
            else if (next == '.' || next == '-' || next is >= '0' and <= '9')
            {
                var j = i;
                while (true)
                {
                    j++;
                    if (j >= length)
                    {
                        break;
                    }

                    var current = text[j];
                    if (current == ' ' || current == ',')
                    {
                        break;
                    }
                }

                var value = ParseFloat(text.AsSpan(i, j - i));
                node.Low = value;
                node.High = value;
                node.Distribution = PopCurve.Linear;
                i = j;
            }
            else
            {
                node.Low = 0;
                node.High = 0;
                node.Distribution = PopCurve.Linear;
            }

            if (i >= length)
            {
                node.Time = -10000;
                node.Curve = PopCurve.Linear;
                nodes.Add(node);
                break;
            }

            if (text[i] == ',')
            {
                i++;
                var j = i;
                while (true)
                {
                    j++;
                    if (j >= length || text[j] == ' ')
                    {
                        break;
                    }
                }

                node.Time = ParseFloat(text.AsSpan(i, j - i));
                i = j;
            }
            else
            {
                node.Time = -10000;
            }

            if (++i >= length)
            {
                node.Curve = PopCurve.Linear;
                nodes.Add(node);
                break;
            }

            if (text[i] is < 'A' or > 'Z')
            {
                node.Curve = PopCurve.Linear;
            }
            else
            {
                var j = i;
                while (true)
                {
                    j++;
                    if (j >= length || text[j] == ' ')
                    {
                        break;
                    }
                }

                node.Curve = ParseCurve(text[i..j]);
                i = ++j;
            }

            nodes.Add(node);
        }

        return ApplyDefaultTimes(nodes);
    }

    private static List<ParticleTrackNode> ApplyDefaultTimes(List<ParticleTrackNode> nodes)
    {
        var count = nodes.Count;
        if (count == 0)
        {
            return nodes;
        }

        if (nodes[0].Time < -1000)
        {
            nodes[0].Time = 0;
        }

        if (count != 1 && nodes[count - 1].Time < -1000)
        {
            nodes[count - 1].Time = 100;
        }

        float delta = 0;
        var last = 0f;
        for (var i = 0; i < count; i++)
        {
            if (nodes[i].Time >= -1000)
            {
                last = nodes[i].Time;
                if (i < count - 1)
                {
                    var j = i + 1;
                    while (nodes[j].Time < -1000)
                    {
                        j++;
                    }

                    delta = (nodes[j].Time - nodes[i].Time) / (j - i);
                }
            }
            else
            {
                last += delta;
                nodes[i].Time = last;
            }

            nodes[i].Time /= 100;
        }

        return nodes;
    }


    private static float ParseFloat(ReadOnlySpan<char> text) =>
        float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static PopCurve ParseCurve(string token)
    {
        for (var i = 0; i < ParticleNames.Curves.Length; i++)
        {
            if (string.Equals(ParticleNames.Curves[i], token, StringComparison.Ordinal))
            {
                return (PopCurve)i;
            }
        }

        if (token.StartsWith("TodCurves(", StringComparison.Ordinal) && token.EndsWith(')'))
        {
            var body = token["TodCurves(".Length..^1];
            if (int.TryParse(body, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                return (PopCurve)value;
            }
        }

        throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.InvalidParticleCurve0, token));
    }

    private static string FormatCurve(PopCurve curve)
    {
        var index = (int)curve;
        return index >= 0 && index < ParticleNames.Curves.Length
            ? ParticleNames.Curves[index]
            : $"TodCurves({index})";
    }
}

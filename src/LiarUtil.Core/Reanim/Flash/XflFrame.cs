using System.Xml.Linq;
using LiarUtil.Core.Core.Xml;

namespace LiarUtil.Core.Reanim.Flash;

internal sealed class XflFrame
{
    public XflFrame(XElement node, int? index = null, List<XflElement>? elements = null)
    {
        Node = node;
        Index = index ?? XmlNumbers.ParseInt(XflElement.Attr(node, "index"), 0);
        Duration = XmlNumbers.ParseInt(XflElement.Attr(node, "duration"), 1);
        Elements = elements ?? [.. XflElement.Children(XflElement.Child(node, "elements"), null).Select(element => new XflElement(element))];
    }

    public XElement Node { get; }

    public int Index { get; }

    public int Duration { get; }

    public int End => Index + Duration;

    public string TweenType => XflElement.Attr(Node, "tweenType");

    public List<XflElement> Elements { get; }

    public double ProgressAt(int frame)
    {
        var progress = XflMatrixParts.Clamp01((double)(frame - Index) / Math.Max(1, Duration));
        var points = Node.Descendants()
            .Where(point => point.Name.LocalName == "Point"
                && point.Attribute("x") is not null
                && point.Attribute("y") is not null
                && point.Ancestors().Any(parent => XflEasing.Normalize(parent.Name.LocalName) == "customease"))
            .OrderBy(point => XmlNumbers.ParseDouble(XflElement.Attr(point, "x"), 0))
            .ToList();
        if (points.Count >= 2)
        {
            return ProgressFromPoints(points, progress);
        }

        var fallback = XflElement.Attr(Node, "tweenEasing");
        var fallbackName = XflElement.HasAttr(Node, "easingName") ? XflElement.Attr(Node, "easingName") : fallback;
        if (XflElement.HasAttr(Node, "easing") || XflElement.HasAttr(Node, "tweenEasing") || XflElement.HasAttr(Node, "easingName"))
        {
            return XflEasing.Evaluate(XflElement.Attr(Node, "easing", fallbackName), progress);
        }

        var acceleration = XmlNumbers.ParseDouble(XflElement.Attr(Node, "acceleration"), 0);
        if (acceleration != 0)
        {
            var amount = Math.Min(Math.Abs(acceleration) / 100.0, 1);
            return XflMatrixParts.Lerp(progress, XflEasing.Evaluate(acceleration > 0 ? "quadraticout" : "quadraticin", progress), amount);
        }
        return progress;
    }

    private static double ProgressFromPoints(List<XElement> points, double progress)
    {
        for (var index = 1; index < points.Count; index++)
        {
            var currentX = XmlNumbers.ParseDouble(XflElement.Attr(points[index], "x"), 0);
            if (progress > currentX)
            {
                continue;
            }
            var previousX = XmlNumbers.ParseDouble(XflElement.Attr(points[index - 1], "x"), 0);
            var previousY = XmlNumbers.ParseDouble(XflElement.Attr(points[index - 1], "y"), 0);
            var currentY = XmlNumbers.ParseDouble(XflElement.Attr(points[index], "y"), 0);
            var span = currentX - previousX;
            return XflMatrixParts.Clamp01(span == 0 ? previousY : XflMatrixParts.Lerp(previousY, currentY, (progress - previousX) / span));
        }
        return XflMatrixParts.Clamp01(XmlNumbers.ParseDouble(XflElement.Attr(points[^1], "y"), 0));
    }
}

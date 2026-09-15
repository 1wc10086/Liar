using System.Xml.Linq;

namespace LiarUtil.Core.Reanim.Flash;

internal sealed class XflLayer
{
    private readonly List<XflFrame> _keyframes;

    public XflLayer(XElement node)
    {
        Node = node;
        _keyframes = [.. XflElement.Children(XflElement.Child(node, "frames"), "DOMFrame")
            .Select(frame => new XflFrame(frame))
            .OrderBy(frame => frame.Index)];
    }

    public XElement Node { get; }

    public string Name => XflElement.Attr(Node, "name");

    public int FrameCount => _keyframes.Count == 0 ? 0 : _keyframes.Max(frame => frame.End);

    public XflFrame? FrameAt(int index)
    {
        XflFrame? previous = null;
        foreach (var frame in _keyframes)
        {
            if (frame.Index > index)
            {
                break;
            }
            previous = frame;
            if (index < frame.End)
            {
                return frame;
            }
        }
        return previous;
    }

    public List<XflFrame> BakeFrames(int totalFrames)
    {
        var result = new List<XflFrame>(totalFrames);
        for (var index = 0; index < totalFrames; index++)
        {
            XflFrame? current = null;
            XflFrame? next = null;
            for (var keyIndex = 0; keyIndex < _keyframes.Count; keyIndex++)
            {
                var candidate = _keyframes[keyIndex];
                if (candidate.Index <= index && index < candidate.End)
                {
                    current = candidate;
                    if (keyIndex + 1 < _keyframes.Count)
                    {
                        next = _keyframes[keyIndex + 1];
                    }
                    break;
                }
            }

            var elements = new List<XflElement>();
            if (current is not null)
            {
                var tween = next is not null && next.Index == current.End && current.TweenType == "motion";
                var progress = tween ? current.ProgressAt(index) : 0;
                for (var elementIndex = 0; elementIndex < current.Elements.Count; elementIndex++)
                {
                    var source = current.Elements[elementIndex];
                    var destination = next is not null && elementIndex < next.Elements.Count ? next.Elements[elementIndex] : null;
                    var sameElement = tween && destination is not null
                        && source.Node.Name.LocalName == destination.Node.Name.LocalName
                        && source.LibraryItemName == destination.LibraryItemName;
                    elements.Add(sameElement
                        ? new XflElement(
                            source.Node,
                            Interpolate(source.Matrix, destination!.Matrix, progress),
                            XflMatrixParts.Lerp(source.Alpha, destination.Alpha, progress))
                        : source);
                }
            }
            result.Add(new XflFrame(current?.Node ?? new XElement("DOMFrame"), index, elements));
        }
        return result;
    }

    private static XflMatrix Interpolate(XflMatrix start, XflMatrix end, double progress)
    {
        var first = XflMatrixParts.Decompose(start);
        var last = XflMatrixParts.Decompose(end);
        var endX = XflMatrixParts.Unwrap(first.XAngle, last.XAngle);
        var endY = XflMatrixParts.Unwrap(first.YAngle, last.YAngle);
        var scaleX = XflMatrixParts.Lerp(first.ScaleX, last.ScaleX, progress);
        var scaleY = XflMatrixParts.Lerp(first.ScaleY, last.ScaleY, progress);
        var skewX = XflMatrixParts.Lerp(first.XAngle, endX, progress);
        var skewY = XflMatrixParts.Lerp(first.YAngle, endY, progress);
        return new XflMatrix(
            scaleX * Math.Cos(skewX), scaleX * Math.Sin(skewX),
            scaleY * Math.Sin(skewY), scaleY * Math.Cos(skewY),
            XflMatrixParts.Lerp(start.Tx, end.Tx, progress), XflMatrixParts.Lerp(start.Ty, end.Ty, progress));
    }
}

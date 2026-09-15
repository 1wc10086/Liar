using System.Xml.Linq;

namespace LiarUtil.Core.Reanim.Flash;

internal sealed class XflTimeline
{
    public XflTimeline(XElement? node)
    {
        if (node is null)
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.XFLDocumentLacksTimeline);
        }
        Node = node;
        Layers = [.. XflElement.Children(XflElement.Child(node, "layers"), "DOMLayer").Select(layer => new XflLayer(layer))];
    }

    public XElement Node { get; }

    public List<XflLayer> Layers { get; }

    public int FrameCount => Layers.Count == 0 ? 0 : Layers.Max(layer => layer.FrameCount);
}

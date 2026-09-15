using System.Xml.Linq;
using LiarUtil.Core.Core.Xml;

namespace LiarUtil.Core.Reanim.Flash;

internal sealed class XflElement(XElement node, XflMatrix? matrix = null, double? alpha = null)
{
    public XElement Node { get; } = node;

    public XflMatrix Matrix { get; } = matrix ?? ReadMatrix(node);

    public double Alpha { get; } = alpha ?? ReadAlpha(node);

    public string LibraryItemName => Attr(Node, "libraryItemName");

    public bool IsText => Node.Name.LocalName is "DOMStaticText" or "DOMDynamicText" or "DOMInputText";

    public bool IsInstance => !IsText && Node.Name.LocalName != "DOMShape";

    public static string Attr(XElement? element, string name, string fallback = "") =>
        XmlNodes.Attribute(element, name, fallback);

    public static XElement? Child(XElement? element, string localName) => XmlNodes.Child(element, localName);

    public static IEnumerable<XElement> Children(XElement? element, string? localName) =>
        XmlNodes.Children(element, localName);

    public static bool HasAttr(XElement? element, string name) => XmlNodes.HasAttribute(element, name);

    private static XflMatrix ReadMatrix(XElement node)
    {
        var container = Child(node, "matrix");
        var value = Child(container, "Matrix") ?? container;
        return value is null
            ? XflMatrix.Identity
            : new XflMatrix(
                XmlNumbers.ParseDouble(Attr(value, "a"), 1), XmlNumbers.ParseDouble(Attr(value, "b"), 0),
                XmlNumbers.ParseDouble(Attr(value, "c"), 0), XmlNumbers.ParseDouble(Attr(value, "d"), 1),
                XmlNumbers.ParseDouble(Attr(value, "tx"), 0), XmlNumbers.ParseDouble(Attr(value, "ty"), 0));
    }

    private static double ReadAlpha(XElement node)
    {
        var color = Child(Child(node, "color"), "Color");
        return color is null
            ? 1
            : XmlNumbers.ParseDouble(Attr(color, "alphaMultiplier"), 1) + (XmlNumbers.ParseDouble(Attr(color, "alphaOffset"), 0) / 100.0);
    }
}

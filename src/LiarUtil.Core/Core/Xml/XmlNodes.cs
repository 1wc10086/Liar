using System.Xml.Linq;

namespace LiarUtil.Core.Core.Xml;

public static class XmlNodes
{
    public static string Attribute(XElement? element, string name, string fallback = "") =>
        element?.Attribute(name)?.Value ?? fallback;

    public static string? AttributeOrNull(XElement? element, string name) => element?.Attribute(name)?.Value;

    public static bool HasAttribute(XElement? element, string name) => element?.Attribute(name) is not null;

    public static float AttributeFloat(XElement? element, string name, float fallback = 0f) =>
        XmlNumbers.ParseFloat(AttributeOrNull(element, name), fallback);

    public static double AttributeDouble(XElement? element, string name, double fallback = 0d) =>
        XmlNumbers.ParseDouble(AttributeOrNull(element, name), fallback);

    public static int AttributeInt(XElement? element, string name, int fallback = 0) =>
        XmlNumbers.ParseInt(AttributeOrNull(element, name), fallback);

    public static XElement? Child(XElement? element, string localName) =>
        element?.Elements().FirstOrDefault(child => child.Name.LocalName == localName);

    public static XElement? Child(XElement? element, params string[] path)
    {
        var current = element;
        foreach (var name in path)
        {
            current = Child(current, name);
            if (current is null)
            {
                return null;
            }
        }

        return current;
    }

    public static IEnumerable<XElement> Children(XElement? element, string? localName = null) =>
        element is null
            ? []
            : localName is null
                ? element.Elements()
                : element.Elements().Where(child => child.Name.LocalName == localName);

    public static List<XElement> ChildList(XElement? element, string localName) =>
        [.. Children(element, localName)];

    public static XElement RequireChild(XElement element, string localName, string context) =>
        Child(element, localName) ?? throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.N0Lacks1Node, context, localName));

    public static string ChildValue(XElement? element, string localName) =>
        Child(element, localName)?.Value ?? "";

    public static XName Unqualified(XName name) => XName.Get(name.LocalName);
}

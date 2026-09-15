using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using LiarUtil.Core.Core.IO;
using LiarUtil.Core.Core.Xml;

namespace LiarUtil.Core.Pam.Xfl;

internal static partial class PamXflXml
{
    private static readonly XNamespace XflNamespace = PamXflConstants.Namespace;

    public static XAttribute XsiAttribute => new(XNamespace.Xmlns + "xsi", PamXflConstants.XsiNamespace);

    public static string Number(double value) => value.ToString(PamXflConstants.NumberFormat, CultureInfo.InvariantCulture);

    public static string Scalar(double value) => value.ToString(CultureInfo.InvariantCulture);

    public static XElement Matrix(PamXflMatrix matrix) => new("matrix",
        new XElement("Matrix",
            new XAttribute("a", Number(matrix.A)),
            new XAttribute("b", Number(matrix.B)),
            new XAttribute("c", Number(matrix.C)),
            new XAttribute("d", Number(matrix.D)),
            new XAttribute("tx", Number(matrix.X)),
            new XAttribute("ty", Number(matrix.Y))));

    public static void Save(string path, XElement root)
    {
        FileIO.EnsureDirectory(path);

        foreach (var element in root.DescendantsAndSelf())
        {
            element.Name = XflNamespace + element.Name.LocalName;
        }

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "\t",
            OmitXmlDeclaration = true,
        };
        using var writer = XmlWriter.Create(path, settings);
        new XDocument(new XDeclaration("1.0", "utf-8", null), root).Save(writer);
    }

    public static XElement Read(string path)
    {
        if (!File.Exists(path))
        {
            throw new PamXflException(string.Format(LiarUtil.Core.Strings.XFLFileNotFound0, path));
        }

        var root = XDocument.Load(path).Root ?? throw new PamXflException(string.Format(LiarUtil.Core.Strings.XFLFileContentEmpty0, path));
        foreach (var element in root.DescendantsAndSelf())
        {
            element.Name = element.Name.LocalName;
        }
        return root;
    }

    public static XElement RequireChild(XElement parent, string name, string context) =>
        XmlNodes.Child(parent, name) ?? throw new PamXflException(string.Format(LiarUtil.Core.Strings.N0Lacks1Node, context, name));

    public static XElement? Child(XElement? parent, string name) => XmlNodes.Child(parent, name);

    public static List<XElement> ChildList(XElement? parent, string name) => XmlNodes.ChildList(parent, name);

    public static string Text(XElement element, string name) => XmlNodes.Attribute(element, name);

    public static int Int(XElement element, string name, int fallback) =>
        XmlNumbers.ParseInt(XmlNodes.AttributeOrNull(element, name), fallback);

    public static int RequireInt(XElement element, string name, string context) =>
        XmlNumbers.TryInt(XmlNodes.AttributeOrNull(element, name), out var value)
            ? value
            : throw new PamXflException(string.Format(LiarUtil.Core.Strings.N1AttributeOf0NotValidInteger, context, name));

    public static double Double(XElement element, string name, double fallback) =>
        TryDouble(XmlNodes.Attribute(element, name), fallback);

    public static double TryDouble(string text, double fallback) =>
        XmlNumbers.TryDouble(text.Replace(",", "", StringComparison.Ordinal), out var value) ? value : fallback;

    [GeneratedRegex(PamXflConstants.ImageSymbolPattern)]
    public static partial Regex ImageSymbolPattern();

    [GeneratedRegex(PamXflConstants.CommandPattern)]
    public static partial Regex CommandPattern();
}

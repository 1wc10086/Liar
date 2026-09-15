using System.Globalization;
using System.Xml;
using LiarUtil.Core.Atlas;
using LiarUtil.Core.Core.IO;

namespace LiarUtil.Core.Atlas.Formats;

internal static class XmlHelper
{
    public static XmlDocument Load(string path)
    {
        var document = new XmlDocument { XmlResolver = null };
        document.Load(path);
        return document;
    }

    public static void Save(XmlDocument document, string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        document.Save(path);
    }

    public static string Attribute(XmlNode? node, string name) => node?.Attributes?[name]?.Value ?? "";

    public static XmlNode? FindRoot(XmlDocument document, string path) =>
        document.SelectSingleNode(path) ?? throw new AtlasException(string.Format(LiarUtil.Core.Strings.N0NodeNotFound, path));

    public static string PathStem(string path) =>
        Path.GetFileNameWithoutExtension(FileIO.ToPortablePath(path)).ToLowerInvariant();

    public static int ToInt(string value, string name) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new AtlasException(string.Format(LiarUtil.Core.Strings.N0NotValidInteger, name));

    public static int ToInt(XmlNode node, string name) => ToInt(Attribute(node, name), name);

    public static int OptionalInt(XmlNode node, string name) =>
        node.Attributes?[name] is null ? 0 : ToInt(Attribute(node, name), name);

    public static bool Has(XmlNode node, string name) => node.Attributes?[name] is not null;

    public static void SetAttribute(XmlNode node, string name, int value) => SetAttribute(node, name, value.ToString());

    public static void SetAttribute(XmlNode node, string name, string value)
    {
        var attribute = node.Attributes?[name];
        if (attribute is null)
        {
            attribute = node.OwnerDocument!.CreateAttribute(name);
            node.Attributes!.Append(attribute);
        }

        attribute.Value = value;
    }
}

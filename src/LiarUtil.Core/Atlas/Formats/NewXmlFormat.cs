using System.Xml;
using LiarUtil.Core.Atlas;

namespace LiarUtil.Core.Atlas.Formats;

internal sealed class NewXmlFormat : IAtlasFormat
{
    private const string AtlasImageType = "2";
    private const string SpriteImageType = "4";

    public AtlasReadResult Read(string infoPath, string inputFile, string? itemName)
    {
        var document = XmlHelper.Load(infoPath);
        var root = XmlHelper.FindRoot(document, "/ResourceManifest");
        var stem = XmlHelper.PathStem(inputFile);
        var found = FindGroup(root, itemName, stem);
        var id = string.IsNullOrEmpty(itemName) ? XmlHelper.Attribute(found.Atlas, "id") : itemName;
        var parent = id.Split('|')[0];
        var sprites = new List<AtlasSubImage>();
        foreach (XmlNode node in found.Group.ChildNodes)
        {
            if (IsSprite(node, parent))
            {
                sprites.Add(new AtlasSubImage(
                    XmlHelper.Attribute(node, "id").Split('|')[0],
                    XmlHelper.OptionalInt(node, "ax"),
                    XmlHelper.OptionalInt(node, "ay"),
                    XmlHelper.ToInt(node, "aw"),
                    XmlHelper.ToInt(node, "ah")));
            }
        }

        if (sprites.Count == 0)
        {
            throw new AtlasException(LiarUtil.Core.Strings.AtlasContainsNoSubImages);
        }

        return new AtlasReadResult(id, sprites);
    }

    public void Write(string infoPath, string outputFile, string? itemName, int width, int height, IReadOnlyDictionary<string, AtlasSubImage> subImages)
    {
        var document = XmlHelper.Load(infoPath);
        var root = XmlHelper.FindRoot(document, "/ResourceManifest");
        var stem = XmlHelper.PathStem(outputFile);
        var found = FindGroup(root, itemName, stem);
        var id = string.IsNullOrEmpty(itemName) ? XmlHelper.Attribute(found.Atlas, "id") : itemName;
        var parent = id.Split('|')[0];
        XmlHelper.SetAttribute(found.Atlas, "aw", width);
        XmlHelper.SetAttribute(found.Atlas, "ah", height);
        foreach (XmlNode node in found.Group.ChildNodes)
        {
            if (!IsSprite(node, parent))
            {
                continue;
            }

            var key = XmlHelper.Attribute(node, "id").Split('|')[0].ToLowerInvariant();
            if (!subImages.TryGetValue(key, out var subImage))
            {
                throw new AtlasException(string.Format(LiarUtil.Core.Strings.MissingInputImage0, key));
            }

            SetOptionalAttribute(node, "ax", subImage.X);
            SetOptionalAttribute(node, "ay", subImage.Y);
            XmlHelper.SetAttribute(node, "aw", subImage.Width);
            XmlHelper.SetAttribute(node, "ah", subImage.Height);
        }

        XmlHelper.Save(document, infoPath);
    }

    private static (XmlNode Group, XmlNode Atlas) FindGroup(XmlNode root, string? itemName, string stem)
    {
        foreach (XmlNode group in root.ChildNodes)
        {
            foreach (XmlNode node in group.ChildNodes)
            {
                if (XmlHelper.Attribute(node, "type") != "0" || XmlHelper.Attribute(node, "imagetype") != AtlasImageType)
                {
                    continue;
                }

                var id = XmlHelper.Attribute(node, "id");
                if (!string.IsNullOrEmpty(itemName) ? id == itemName : XmlHelper.PathStem(XmlHelper.Attribute(node, "path")) == stem)
                {
                    return (group, node);
                }
            }
        }

        throw new AtlasException(LiarUtil.Core.Strings.AtlasInfoNotFound);
    }

    private static bool IsSprite(XmlNode node, string parent) =>
        XmlHelper.Attribute(node, "type") == "0" &&
        XmlHelper.Attribute(node, "imagetype") == SpriteImageType &&
        XmlHelper.Attribute(node, "parent") == parent;

    private static void SetOptionalAttribute(XmlNode node, string name, int value)
    {
        if (value == 0)
        {
            if (XmlHelper.Has(node, name))
            {
                node.Attributes!.RemoveNamedItem(name);
            }

            return;
        }

        XmlHelper.SetAttribute(node, name, value);
    }
}

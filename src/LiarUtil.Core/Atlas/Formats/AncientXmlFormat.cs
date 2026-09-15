using System.Xml;
using LiarUtil.Core.Atlas;

namespace LiarUtil.Core.Atlas.Formats;

internal sealed class AncientXmlFormat : IAtlasFormat
{
    public AtlasReadResult Read(string infoPath, string inputFile, string? itemName)
    {
        var document = XmlHelper.Load(infoPath);
        var root = XmlHelper.FindRoot(document, "/ResourceManifest");
        var atlas = FindAtlas(root, itemName, XmlHelper.PathStem(inputFile));
        var id = string.IsNullOrEmpty(itemName) ? XmlHelper.Attribute(atlas, "id") : itemName;
        var prefix = "";
        var subImages = new List<AtlasSubImage>();
        foreach (XmlNode node in atlas.ChildNodes)
        {
            if (node.Name == "SetDefaults")
            {
                prefix = XmlHelper.Attribute(node, "idprefix");
                continue;
            }

            subImages.Add(new AtlasSubImage(
                prefix + XmlHelper.Attribute(node, "id"),
                XmlHelper.ToInt(node, "x"),
                XmlHelper.ToInt(node, "y"),
                XmlHelper.ToInt(node, "width"),
                XmlHelper.ToInt(node, "height")));
        }

        if (subImages.Count == 0)
        {
            throw new AtlasException(LiarUtil.Core.Strings.AtlasContainsNoSubImages);
        }

        return new AtlasReadResult(id, subImages);
    }

    public void Write(string infoPath, string outputFile, string? itemName, int width, int height, IReadOnlyDictionary<string, AtlasSubImage> subImages)
    {
        var document = XmlHelper.Load(infoPath);
        var root = XmlHelper.FindRoot(document, "/ResourceManifest");
        var atlas = FindAtlas(root, itemName, XmlHelper.PathStem(outputFile));
        var prefix = "";
        foreach (XmlNode node in atlas.ChildNodes)
        {
            if (node.Name == "SetDefaults")
            {
                prefix = XmlHelper.Attribute(node, "idprefix");
                continue;
            }

            var key = (prefix + XmlHelper.Attribute(node, "id")).ToLowerInvariant();
            if (!subImages.TryGetValue(key, out var subImage))
            {
                throw new AtlasException(string.Format(LiarUtil.Core.Strings.MissingInputImage0, key));
            }

            XmlHelper.SetAttribute(node, "x", subImage.X);
            XmlHelper.SetAttribute(node, "y", subImage.Y);
            XmlHelper.SetAttribute(node, "width", subImage.Width);
            XmlHelper.SetAttribute(node, "height", subImage.Height);
        }

        XmlHelper.Save(document, infoPath);
    }

    private static XmlNode FindAtlas(XmlNode root, string? itemName, string stem)
    {
        foreach (XmlNode group in root.ChildNodes)
        {
            var prefix = "";
            foreach (XmlNode node in group.ChildNodes)
            {
                if (node.Name == "SetDefaults")
                {
                    prefix = XmlHelper.Attribute(node, "idprefix");
                    continue;
                }

                if (node.Name != "Atlas")
                {
                    continue;
                }

                var matches = string.IsNullOrEmpty(itemName)
                    ? XmlHelper.PathStem(XmlHelper.Attribute(node, "path")) == stem
                    : prefix + XmlHelper.Attribute(node, "id") == itemName;
                if (matches)
                {
                    return node;
                }
            }
        }

        throw new AtlasException(LiarUtil.Core.Strings.AtlasInfoNotFound);
    }
}

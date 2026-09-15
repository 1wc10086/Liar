using System.Xml;
using LiarUtil.Core.Atlas;

namespace LiarUtil.Core.Atlas.Formats;

internal sealed class OldXmlFormat : IAtlasFormat
{
    public AtlasReadResult Read(string infoPath, string inputFile, string? itemName)
    {
        var document = XmlHelper.Load(infoPath);
        var root = XmlHelper.FindRoot(document, "/resources-manifest");
        var atlas = FindAtlas(root, itemName, XmlHelper.PathStem(inputFile));
        var id = string.IsNullOrEmpty(itemName) ? XmlHelper.Attribute(atlas, "id") : itemName;
        var subImages = new List<AtlasSubImage>();
        foreach (XmlNode node in atlas.ChildNodes)
        {
            subImages.Add(new AtlasSubImage(
                XmlHelper.Attribute(node, "id"),
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
        var root = XmlHelper.FindRoot(document, "/resources-manifest");
        var atlas = FindAtlas(root, itemName, XmlHelper.PathStem(outputFile));
        foreach (XmlNode node in atlas.ChildNodes)
        {
            var key = XmlHelper.Attribute(node, "id").ToLowerInvariant();
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
            foreach (XmlNode node in group.ChildNodes)
            {
                if (node.Name != "atlas")
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(itemName)
                        ? XmlHelper.Attribute(node, "id") == itemName
                        : XmlHelper.PathStem(XmlHelper.Attribute(node, "path")) == stem)
                {
                    return node;
                }
            }
        }

        throw new AtlasException(LiarUtil.Core.Strings.AtlasInfoNotFound);
    }
}

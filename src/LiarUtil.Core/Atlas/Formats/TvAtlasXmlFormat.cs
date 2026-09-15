using System.Xml;
using LiarUtil.Core.Atlas;

namespace LiarUtil.Core.Atlas.Formats;

internal sealed class TvAtlasXmlFormat : IAtlasFormat
{
    public AtlasReadResult Read(string infoPath, string inputFile, string? itemName)
    {
        var document = XmlHelper.Load(infoPath);
        var root = XmlHelper.FindRoot(document, "/atlases");
        var atlas = root.ChildNodes.Count == 0 ? throw new AtlasException(LiarUtil.Core.Strings.AtlasInfoNotFound) : root.ChildNodes[0]!;
        var subImages = new List<AtlasSubImage>();
        foreach (XmlNode node in atlas.ChildNodes)
        {
            subImages.Add(new AtlasSubImage(
                XmlHelper.PathStem(XmlHelper.Attribute(node, "name")),
                XmlHelper.ToInt(node, "x"),
                XmlHelper.ToInt(node, "y"),
                XmlHelper.ToInt(node, "w"),
                XmlHelper.ToInt(node, "h")));
        }

        if (subImages.Count == 0)
        {
            throw new AtlasException(LiarUtil.Core.Strings.AtlasContainsNoSubImages);
        }

        return new AtlasReadResult(null, subImages);
    }

    public void Write(string infoPath, string outputFile, string? itemName, int width, int height, IReadOnlyDictionary<string, AtlasSubImage> subImages)
    {
        var document = XmlHelper.Load(infoPath);
        var root = XmlHelper.FindRoot(document, "/atlases");
        var atlas = root.ChildNodes.Count == 0 ? throw new AtlasException(LiarUtil.Core.Strings.AtlasInfoNotFound) : root.ChildNodes[0]!;
        foreach (XmlNode node in atlas.ChildNodes)
        {
            var key = XmlHelper.PathStem(XmlHelper.Attribute(node, "name"));
            if (!subImages.TryGetValue(key, out var subImage))
            {
                throw new AtlasException(string.Format(LiarUtil.Core.Strings.MissingInputImage0, key));
            }

            XmlHelper.SetAttribute(node, "x", subImage.X);
            XmlHelper.SetAttribute(node, "y", subImage.Y);
            XmlHelper.SetAttribute(node, "w", subImage.Width);
            XmlHelper.SetAttribute(node, "h", subImage.Height);
        }

        XmlHelper.Save(document, infoPath);
    }
}

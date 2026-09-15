using System.Globalization;
using System.Xml;
using LiarUtil.Core.Atlas;

namespace LiarUtil.Core.Atlas.Formats;

internal sealed class PlistFormat : IAtlasFormat
{
    public AtlasReadResult Read(string infoPath, string inputFile, string? itemName)
    {
        var document = XmlHelper.Load(infoPath);
        var frames = FindFrames(document);
        var subImages = new List<AtlasSubImage>();
        foreach (var (key, body) in EnumerateFrames(frames))
        {
            var textureRect = Key(body, "textureRect")?.InnerText;
            if (string.IsNullOrWhiteSpace(textureRect))
            {
                throw new AtlasException(string.Format(LiarUtil.Core.Strings.PlistEntryLacksTextureRect0, key));
            }

            var numbers = textureRect.Split([',', '{', '}'], StringSplitOptions.RemoveEmptyEntries);
            if (numbers.Length < 4)
            {
                throw new AtlasException(string.Format(LiarUtil.Core.Strings.PlistTextureRectInvalid0, textureRect));
            }

            var values = new int[4];
            for (var i = 0; i < 4; i++)
            {
                values[i] = int.Parse(numbers[i], CultureInfo.InvariantCulture);
            }

            subImages.Add(new AtlasSubImage(
                XmlHelper.PathStem(key),
                values[0],
                values[1],
                values[2],
                values[3],
                Key(body, "textureRotated")?.Name == "true"));
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
        var frames = FindFrames(document);
        foreach (var (key, body) in EnumerateFrames(frames))
        {
            var id = XmlHelper.PathStem(key);
            if (!subImages.TryGetValue(id, out var subImage))
            {
                throw new AtlasException(string.Format(LiarUtil.Core.Strings.MissingInputImage0, id));
            }

            SetValue(body, "spriteOffset", "{0,0}");
            SetValue(body, "spriteSize", $"{{{subImage.Width},{subImage.Height}}}");
            SetValue(body, "spriteSourceSize", $"{{{subImage.Width},{subImage.Height}}}");
            SetValue(body, "textureRect", $"{{{{{subImage.X},{subImage.Y}}},{{{subImage.Width},{subImage.Height}}}}}");
            if (Key(body, "textureRotated") is { } rotated)
            {
                body.ReplaceChild(document.CreateElement("false"), rotated);
            }
        }

        XmlHelper.Save(document, infoPath);
    }

    private static XmlNode FindFrames(XmlDocument document)
    {
        var root = XmlHelper.FindRoot(document, "/plist");
        var dict = root.ChildNodes.Count > 0 ? root.ChildNodes[0]! : throw new AtlasException(LiarUtil.Core.Strings.PlistStructureInvalid);
        foreach (XmlNode node in dict.ChildNodes)
        {
            if (node.Name == "key" && node.InnerText.Trim() == "frames" && node.NextSibling is not null)
            {
                return node.NextSibling;
            }
        }

        throw new AtlasException(LiarUtil.Core.Strings.PlistFramesDictionaryNotFound);
    }

    private static IEnumerable<(string Key, XmlNode Body)> EnumerateFrames(XmlNode frames)
    {
        for (var node = frames.FirstChild; node is not null; node = node.NextSibling)
        {
            if (node.Name == "key" && node.NextSibling is { } body && body.Name == "dict")
            {
                yield return (node.InnerText.Trim(), body);
            }
        }
    }

    private static XmlNode? Key(XmlNode body, string name)
    {
        for (var node = body.FirstChild; node is not null; node = node.NextSibling)
        {
            if (node.Name == "key" && node.InnerText.Trim() == name)
            {
                return node.NextSibling;
            }
        }

        return null;
    }

    private static void SetValue(XmlNode body, string name, string value)
    {
        if (Key(body, name) is { } target)
        {
            target.InnerText = value;
        }
    }
}

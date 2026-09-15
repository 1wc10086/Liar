namespace LiarUtil.Core.Atlas.Formats;

internal sealed class ImageDatFormat : IAtlasFormat
{
    public AtlasReadResult Read(string infoPath, string inputFile, string? itemName)
    {
        var entries = ImageDatFile.Read(infoPath);
        var name = ResolveName(entries, itemName, XmlHelper.PathStem(inputFile));
        if (string.IsNullOrEmpty(name))
        {
            throw new AtlasException(LiarUtil.Core.Strings.AtlasimagemapDatRequiresAtlasName);
        }

        var subImages = new List<AtlasSubImage>();
        foreach (var entry in entries)
        {
            if (string.Equals(entry.Parent, name, StringComparison.OrdinalIgnoreCase))
            {
                subImages.Add(new AtlasSubImage(entry.Id, entry.X, entry.Y, entry.Width, entry.Height));
            }
        }

        if (subImages.Count == 0)
        {
            throw new AtlasException(LiarUtil.Core.Strings.AtlasContainsNoSubImages);
        }

        return new AtlasReadResult(name, subImages);
    }

    public void Write(string infoPath, string outputFile, string? itemName, int width, int height, IReadOnlyDictionary<string, AtlasSubImage> subImages)
    {
        var name = ResolveName(ImageDatFile.Read(infoPath), itemName, XmlHelper.PathStem(outputFile));
        if (string.IsNullOrEmpty(name))
        {
            throw new AtlasException(LiarUtil.Core.Strings.AtlasimagemapDatRequiresAtlasName);
        }

        ImageDatFile.Update(infoPath, name, subImages);
    }

    private static string ResolveName(IReadOnlyList<ImageDatEntry> entries, string? itemName, string stem)
    {
        if (!string.IsNullOrEmpty(itemName))
        {
            return itemName;
        }

        foreach (var entry in entries)
        {
            if (string.Equals(entry.Parent, stem, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entry.Id, stem, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Parent;
            }
        }

        var parents = entries.Select(entry => entry.Parent).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return parents.Count == 1 ? parents[0] : "";
    }
}

using LiarUtil.Core.Core.IO;

namespace LiarUtil.Core.Atlas;

internal static class AtlasCutter
{
    public static void Cut(string inputPath, string outputFolder, IReadOnlyList<AtlasSubImage> subImages)
    {
        using var atlas = AtlasBitmap.Load(inputPath);
        Directory.CreateDirectory(outputFolder);
        foreach (var subImage in subImages)
        {
            using var sprite = Cut(atlas, subImage);
            var path = Path.Combine(outputFolder, $"{subImage.Id.ToLowerInvariant()}.png");
            sprite.Save(path);
        }
    }

    private static AtlasBitmap Cut(AtlasBitmap atlas, AtlasSubImage subImage)
    {
        var sourceWidth = subImage.Rotated ? subImage.Height : subImage.Width;
        var sourceHeight = subImage.Rotated ? subImage.Width : subImage.Height;
        if (subImage.Width <= 0 || subImage.Height <= 0 ||
            subImage.X + sourceWidth > atlas.Width || subImage.Y + sourceHeight > atlas.Height)
        {
            throw new AtlasException(string.Format(LiarUtil.Core.Strings.SubImageExceedsAtlasBounds0, subImage.Id));
        }

        var source = atlas.Cut(subImage.X, subImage.Y, sourceWidth, sourceHeight);
        if (!subImage.Rotated)
        {
            return source;
        }

        using (source)
        {
            return source.Rotate270();
        }
    }
}

internal static class AtlasIdFile
{
    private const string FileName = "AtlasID.txt";

    public static string Read(string folder)
    {
        var path = Path.Combine(folder, FileName);
        return File.Exists(path) ? FileIO.ReadAllText(path).Replace("\r", "").Replace("\n", "") : "";
    }

    public static void Write(string folder, string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            FileIO.WriteAllText(Path.Combine(folder, FileName), id);
        }
    }
}

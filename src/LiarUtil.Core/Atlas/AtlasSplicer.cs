using LiarUtil.Core.Atlas.Packing;

namespace LiarUtil.Core.Atlas;

internal static class AtlasSplicer
{
    public static Dictionary<string, AtlasSubImage> Splice(string inputFolder, string outputPath, int width, int height)
    {
        if (!Directory.Exists(inputFolder))
        {
            throw new AtlasException(string.Format(LiarUtil.Core.Strings.InputDirectoryNotFound0, inputFolder));
        }

        if (width <= 0 || height <= 0)
        {
            throw new AtlasException(LiarUtil.Core.Strings.AtlasDimensionsMustPositive);
        }

        var images = new List<(AtlasBitmap Bitmap, string Id)>();
        try
        {
            foreach (var path in Directory.GetFiles(inputFolder))
            {
                if (!string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                images.Add((AtlasBitmap.Load(path), Path.GetFileNameWithoutExtension(path).ToLowerInvariant()));
            }

            if (images.Count == 0)
            {
                throw new AtlasException(LiarUtil.Core.Strings.InputDirectoryContainsNoPNGFiles);
            }

            images.Sort((a, b) => (b.Bitmap.Width * b.Bitmap.Height) - (a.Bitmap.Width * a.Bitmap.Height));
            var packer = new MaxRectsBinPack(width, height, false);
            var result = new Dictionary<string, AtlasSubImage>(images.Count, StringComparer.Ordinal);
            using var atlas = new AtlasBitmap(width, height);
            foreach (var (bitmap, id) in images)
            {
                var rect = packer.Insert(bitmap.Width, bitmap.Height, MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestAreaFit);
                if (rect.Height == 0)
                {
                    throw new AtlasException(string.Format(LiarUtil.Core.Strings.ImageDoesNotFitIntoSpecifiedAtlasSize, id));
                }

                atlas.Blit(bitmap, rect.X, rect.Y);
                result[id] = new AtlasSubImage(id, rect.X, rect.Y, bitmap.Width, bitmap.Height);
            }

            atlas.Save(outputPath);
            return result;
        }
        finally
        {
            foreach (var (bitmap, _) in images)
            {
                bitmap.Dispose();
            }
        }
    }
}

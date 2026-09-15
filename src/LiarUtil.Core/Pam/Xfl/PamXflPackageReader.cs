using System.Text.Json;
using System.Xml.Linq;

namespace LiarUtil.Core.Pam.Xfl;

internal static class PamXflPackageReader
{
    public static PamAnimation Read(string inputFolder)
    {
        var root = Path.GetFullPath(inputFolder);
        var extraPath = Path.Combine(root, PamXflConstants.ExtraFileName);
        if (!File.Exists(extraPath))
        {
            throw new PamXflException(string.Format(LiarUtil.Core.Strings.XFLManifestFileNotFound0, extraPath));
        }

        var extra = JsonSerializer.Deserialize(File.ReadAllText(extraPath), PamXflExtraContext.Default.PamXflExtra)
            ?? throw new PamXflException(LiarUtil.Core.Strings.XFLExtraJsonContentEmpty);
        var document = PamXflXml.Read(Path.Combine(root, PamXflDocument.FileName));
        var animation = new PamAnimation
        {
            Version = extra.Version is >= 1 and <= 6 ? extra.Version : PamXflConstants.DefaultVersion,
            FrameRate = (int)Math.Round(PamXflXml.Double(document, "frameRate", PamXflConstants.DefaultFrameRate)),
            PositionX = At(extra.Position, 0),
            PositionY = At(extra.Position, 1),
        };

        var library = Path.Combine(root, PamXflConstants.LibraryFolder);
        ReadImages(library, extra, animation);
        ReadSprites(library, extra, animation);
        PamXflDocumentReader.Read(document, animation);
        return animation;
    }

    private static void ReadImages(string library, PamXflExtra extra, PamAnimation animation)
    {
        for (var index = 0; index < extra.Image.Count; index++)
        {
            var path = Path.Combine(library, PamXflConstants.ImageFolder, PamXflNames.ImageFile(index));
            animation.Images.Add(PamXflImageReader.Read(PamXflXml.Read(path), extra.Image[index], index, animation.Version));
        }
    }

    private static void ReadSprites(string library, PamXflExtra extra, PamAnimation animation)
    {
        for (var index = 0; index < extra.Sprite.Count; index++)
        {
            var path = Path.Combine(library, PamXflConstants.SpriteFolder, PamXflNames.SpriteFile(index));
            animation.Sprites.Add(PamXflSpriteReader.Read(
                PamXflXml.Read(path), extra.Sprite[index], index, animation.FrameRate));
        }

        if (extra.MainSprite is { } mainExtra)
        {
            animation.MainSprite = PamXflSpriteReader.Read(
                PamXflXml.Read(Path.Combine(library, PamXflConstants.MainSpriteFileName)),
                mainExtra,
                null,
                animation.FrameRate);
        }
    }

    private static float At(double[] values, int index) => values.Length > index ? (float)values[index] : 0;
}

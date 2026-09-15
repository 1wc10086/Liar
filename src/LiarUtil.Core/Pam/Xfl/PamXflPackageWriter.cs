using System.Text.Json;
using LiarUtil.Core.Core.IO;

namespace LiarUtil.Core.Pam.Xfl;

internal static class PamXflPackageWriter
{
    public static void Write(PamAnimation animation, string outputFolder, int resolution)
    {
        var root = Path.GetFullPath(outputFolder);
        var library = Path.Combine(root, PamXflConstants.LibraryFolder);
        FileIO.EnsureDirectory(Path.Combine(library, PamXflConstants.ImageFolder));
        FileIO.EnsureDirectory(Path.Combine(library, PamXflConstants.SpriteFolder));
        FileIO.EnsureDirectory(Path.Combine(library, PamXflConstants.SourceFolder));
        FileIO.EnsureDirectory(Path.Combine(library, PamXflConstants.MediaFolder));

        FileIO.WriteAllText(Path.Combine(root, PamXflConstants.ContentFileName), PamXflConstants.ContentFileValue);
        PamXflXml.Save(Path.Combine(root, PamXflDocument.FileName), PamXflDocumentWriter.Create(animation));
        FileIO.WriteAllText(Path.Combine(root, PamXflConstants.ExtraFileName),
            JsonSerializer.Serialize(PamXflExtraWriter.Create(animation), PamXflExtraContext.Default.PamXflExtra));

        WriteImages(library, animation.Images, resolution);
        WriteSprites(library, animation);
    }

    private static void WriteImages(string library, IReadOnlyList<PamImage> images, int resolution)
    {
        for (var index = 0; index < images.Count; index++)
        {
            var image = images[index];
            PamXflXml.Save(Path.Combine(library, PamXflConstants.ImageFolder, PamXflNames.ImageFile(index)),
                PamXflImageWriter.Create(image, index));
            PamXflXml.Save(Path.Combine(library, PamXflConstants.SourceFolder, PamXflNames.SourceFile(index)),
                PamXflSourceWriter.Create(image, index, resolution));
            WriteDefaultMedia(library, image);
        }
    }

    private static void WriteSprites(string library, PamAnimation animation)
    {
        for (var index = 0; index < animation.Sprites.Count; index++)
        {
            PamXflXml.Save(Path.Combine(library, PamXflConstants.SpriteFolder, PamXflNames.SpriteFile(index)),
                PamXflSpriteWriter.Create(animation.Sprites[index], index, animation.Sprites));
        }

        if (animation.MainSprite is { } mainSprite)
        {
            PamXflXml.Save(Path.Combine(library, PamXflConstants.MainSpriteFileName),
                PamXflSpriteWriter.Create(mainSprite, null, animation.Sprites));
        }
    }

    private static void WriteDefaultMedia(string library, PamImage image)
    {
        var path = Path.Combine(library, PamXflConstants.MediaFolder,
            PamXflNames.MediaName(image.Name) + PamXflConstants.ImageFileSuffix);
        if (!File.Exists(path))
        {
            FileIO.WriteAllBytes(path, Reanim.Flash.DefaultBitmap.Bytes);
        }
    }
}

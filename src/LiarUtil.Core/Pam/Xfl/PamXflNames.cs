namespace LiarUtil.Core.Pam.Xfl;

internal static class PamXflNames
{
    public static string MediaName(string imageName)
    {
        var separator = imageName.IndexOf(PamXflConstants.NameSeparator);
        return separator < 0 ? imageName : imageName[..separator];
    }

    public static string ImageSymbol(int index) => PamXflConstants.ImageSymbolPrefix + (index + 1);

    public static string SpriteSymbol(int index) => PamXflConstants.SpriteSymbolPrefix + (index + 1);

    public static string SourceSymbol(int index) => PamXflConstants.SourceSymbolPrefix + (index + 1);

    public static string ImageFile(int index) => "image_" + (index + 1) + PamXflConstants.IncludeFileSuffix;

    public static string SpriteFile(int index) => "sprite_" + (index + 1) + PamXflConstants.IncludeFileSuffix;

    public static string SourceFile(int index) => "source_" + (index + 1) + PamXflConstants.IncludeFileSuffix;
}

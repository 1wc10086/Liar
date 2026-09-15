namespace LiarUtil.Core.Pam.Xfl;

internal static class PamXflConstants
{
    public const int DefaultResolution = 768;

    public const int StandardResolution = 1200;

    public const int DefaultVersion = 6;

    public const int DefaultFrameRate = 30;

    public const string Namespace = "http://ns.adobe.com/xfl/2008/";

    public const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";

    public const string DocumentVersion = "2.971";

    public const string ContentFileName = "main.xfl";

    public const string ContentFileValue = "PROXY-CS5";

    public const string ExtraFileName = "extra.json";

    public const string LibraryFolder = "LIBRARY";

    public const string ImageFolder = "image";

    public const string SpriteFolder = "sprite";

    public const string SourceFolder = "source";

    public const string MediaFolder = "media";

    public const string MainSpriteFileName = "main_sprite.xml";

    public const string MainSpriteName = "main_sprite";

    public const string AnimationTimelineName = "animation";

    public const string FlowLayerName = "flow";

    public const string CommandLayerName = "command";

    public const string SpriteLayerName = "sprite";

    public const string ImageSymbolPrefix = "image/image_";

    public const string SpriteSymbolPrefix = "sprite/sprite_";

    public const string SourceSymbolPrefix = "source/source_";

    public const string MediaPrefix = "media/";

    public const string ImageFileSuffix = ".png";

    public const string IncludeFileSuffix = ".xml";

    public const string StopScript = "stop();";

    public const string NameLabelType = "name";

    public const string SymbolType = "graphic";

    public const string LoopType = "loop";

    public const string NumberFormat = "0.000000";

    public const char NameSeparator = '|';

    public const string ImageSymbolPattern = "(image|sprite)/(image|sprite)_([0-9]+)";

    public const string CommandPattern = "fscommand\\(\"(.*)\", \"(.*)\"\\);";

    public const string CommandFormat = "fscommand(\"{0}\", \"{1}\");";

    public static PamXflColor InitialColor { get; } = new(1, 1, 1, 1);
}

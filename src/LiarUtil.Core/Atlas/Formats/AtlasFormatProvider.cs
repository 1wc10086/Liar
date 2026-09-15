using LiarUtil.Core.Atlas;
namespace LiarUtil.Core.Atlas.Formats;

public static class AtlasFormatProvider
{
    public static void Cut(AtlasFormatKind kind, string inputFile, string outputFolder, string infoPath, string? itemName)
    {
        var result = Create(kind).Read(infoPath, inputFile, itemName);
        AtlasCutter.Cut(inputFile, outputFolder, result.SubImages);
        AtlasIdFile.Write(outputFolder, result.ItemId ?? "");
    }

    public static void Splice(AtlasFormatKind kind, string inputFolder, string outputFile, string infoPath, string? itemName, int width, int height)
    {
        var subImages = AtlasSplicer.Splice(inputFolder, outputFile, width, height);
        var name = string.IsNullOrEmpty(itemName) ? AtlasIdFile.Read(inputFolder) : itemName;
        Create(kind).Write(infoPath, outputFile, name, width, height, subImages);
    }

    public static AtlasFormatKind Parse(int index) => index switch
    {
        1 => AtlasFormatKind.OldXml,
        2 => AtlasFormatKind.AncientXml,
        3 => AtlasFormatKind.Plist,
        4 => AtlasFormatKind.ImageDat,
        5 => AtlasFormatKind.TvAtlasXml,
        6 => AtlasFormatKind.ResJson,
        _ => AtlasFormatKind.NewXml,
    };

    private static IAtlasFormat Create(AtlasFormatKind kind) => kind switch
    {
        AtlasFormatKind.NewXml => new NewXmlFormat(),
        AtlasFormatKind.OldXml => new OldXmlFormat(),
        AtlasFormatKind.AncientXml => new AncientXmlFormat(),
        AtlasFormatKind.Plist => new PlistFormat(),
        AtlasFormatKind.ImageDat => new ImageDatFormat(),
        AtlasFormatKind.TvAtlasXml => new TvAtlasXmlFormat(),
        _ => new ResJsonFormat(),
    };
}

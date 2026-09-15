using LiarUtil.Core.Atlas;
namespace LiarUtil.Core.Atlas.Formats;

internal sealed record AtlasReadResult(string? ItemId, IReadOnlyList<AtlasSubImage> SubImages);

internal interface IAtlasFormat
{
    AtlasReadResult Read(string infoPath, string inputFile, string? itemName);

    void Write(string infoPath, string outputFile, string? itemName, int width, int height, IReadOnlyDictionary<string, AtlasSubImage> subImages);
}

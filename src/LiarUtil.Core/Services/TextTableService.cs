using LiarUtil.Core.TextTable;

namespace LiarUtil.Core.Services;

public sealed class TextTableService
{
    public void Convert(string inputPath, string outputPath, int destinationVersion)
    {
        var source = File.ReadAllBytes(inputPath);
        var result = TextTableConverter.Convert(source, ToVersion(destinationVersion));
        FileHelper.WriteText(outputPath, result);
    }

    private static TextTableVersion ToVersion(int value) => value switch
    {
        1 => TextTableVersion.JsonMap,
        2 => TextTableVersion.JsonList,
        _ => TextTableVersion.Text,
    };
}

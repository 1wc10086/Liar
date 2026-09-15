using LiarUtil.Core.Pam;
using LiarUtil.Core.Pam.Xfl;

namespace LiarUtil.Core.Services;

public sealed class PamService
{
    public void Decode(string inputPath, string outputPath) =>
        FileHelper.WriteText(outputPath, PamCodec.EncodeJson(PamCodec.Decode(File.ReadAllBytes(inputPath))));

    public void Encode(string inputPath, string outputPath, int version) =>
        FileHelper.WriteBytes(outputPath, PamCodec.Encode(PamCodec.DecodeJson(File.ReadAllText(inputPath)), version));

    public void FlashToJson(string inputFolder, string outputPath)
    {
        if (!IsFlashProject(inputFolder))
        {
            throw new PamXflException(string.Format(LiarUtil.Core.Strings.InputNotXFLProjectDirectory0, inputFolder));
        }

        FileHelper.WriteText(ResolveJsonOutput(outputPath, inputFolder),
            PamCodec.EncodeJson(PamXflService.Decode(inputFolder)));
    }

    public void JsonToFlash(string inputPath, string outputFolder, int resolution)
    {
        if (!File.Exists(inputPath))
        {
            throw new PamXflException(string.Format(LiarUtil.Core.Strings.InputNotPAMJSONFile0, inputPath));
        }

        var animation = PamCodec.DecodeJson(File.ReadAllText(inputPath));
        PamXflService.Encode(animation, ResolveFlashOutput(outputFolder, inputPath), resolution);
    }

    private static bool IsFlashProject(string path) =>
        Directory.Exists(path) && File.Exists(Path.Combine(path, PamXflDocument.FileName));

    private static string ResolveJsonOutput(string outputPath, string inputFolder)
    {
        if (!Directory.Exists(outputPath))
        {
            return outputPath;
        }

        return Path.Combine(outputPath, new DirectoryInfo(inputFolder).Name + PamXflDocument.JsonSuffix);
    }

    private static string ResolveFlashOutput(string outputFolder, string inputPath)
    {
        if (!Directory.Exists(outputFolder))
        {
            return outputFolder;
        }

        return Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(inputPath));
    }
}

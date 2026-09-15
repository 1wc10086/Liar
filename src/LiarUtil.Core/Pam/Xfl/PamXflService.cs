namespace LiarUtil.Core.Pam.Xfl;

public static class PamXflService
{
    public static void Encode(PamAnimation animation, string outputFolder, int resolution = PamXflConstants.DefaultResolution)
    {
        if (resolution <= 0)
        {
            throw new PamXflException(string.Format(LiarUtil.Core.Strings.XFLResolutionMustGreaterThan00, resolution));
        }

        PamXflPackageWriter.Write(animation, outputFolder, resolution);
    }

    public static PamAnimation Decode(string inputFolder) => PamXflPackageReader.Read(inputFolder);
}

using LiarUtil.Core.Texture;

namespace LiarUtil.Core.Services;

public sealed class TextureService
{
    public void Decode(int format, string inputPath, string outputPath, string userFormat, bool useHeader, int width, int height, string fmt0Mode, string cdatKey)
    {
        switch (format)
        {
            case 0:
                Ptx.Decode(inputPath, outputPath, useHeader, userFormat, width, height, fmt0Mode);
                break;
            case 1:
                SimpleCodecs.CdatDecode(inputPath, outputPath, cdatKey);
                break;
            case 2:
                SimpleCodecs.TexDecode(inputPath, outputPath);
                break;
            case 3:
                SimpleCodecs.TxzDecode(inputPath, outputPath);
                break;
            case 4:
                PlatformCodecs.TextVDecode(inputPath, outputPath);
                break;
            case 5:
                PlatformCodecs.PtxXbox360Decode(inputPath, outputPath);
                break;
            case 6:
                PlatformCodecs.PtxPs3Decode(inputPath, outputPath);
                break;
            case 7:
                PlatformCodecs.PtxPsvDecode(inputPath, outputPath);
                break;
            case 8:
                SimpleCodecs.XnbDecode(inputPath, outputPath);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
    }

    public void Encode(int format, string inputPath, string outputPath, string textureFormat, bool writeHeader, string fmt0Mode, string cdatKey)
    {
        switch (format)
        {
            case 0:
                WriteAllBytes(outputPath, Ptx.Encode(inputPath, textureFormat, writeHeader, fmt0Mode));
                break;
            case 1:
                SimpleCodecs.CdatEncode(inputPath, outputPath, cdatKey);
                break;
            case 2:
                SimpleCodecs.TexEncode(inputPath, outputPath, textureFormat);
                break;
            case 3:
                SimpleCodecs.TxzEncode(inputPath, outputPath, textureFormat);
                break;
            case 4:
                PlatformCodecs.TextVEncode(inputPath, outputPath, textureFormat);
                break;
            case 5:
                PlatformCodecs.PtxXbox360Encode(inputPath, outputPath);
                break;
            case 6:
                PlatformCodecs.PtxPs3Encode(inputPath, outputPath);
                break;
            case 7:
                PlatformCodecs.PtxPsvEncode(inputPath, outputPath);
                break;
            case 8:
                SimpleCodecs.XnbEncode(inputPath, outputPath);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
    }

    private static void WriteAllBytes(string path, byte[] data)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(path, data);
    }

}

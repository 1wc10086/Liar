using System.Text;
using LiarUtil.Core.Rton;

namespace LiarUtil.Core.Services;

public sealed class RtonService
{
    public void Process(RtonFunction function, string inputPath, string outputPath, StringEncoding encoding, string key)
    {
        var output = function switch
        {
            RtonFunction.Decode => Decode(inputPath, encoding),
            RtonFunction.Encode => Encode(inputPath, encoding),
            RtonFunction.Decrypt => Decrypt(inputPath, key),
            RtonFunction.Encrypt => Encrypt(inputPath, key),
            _ => throw new ArgumentOutOfRangeException(nameof(function)),
        };

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(outputPath, output);
    }

    private static byte[] Decode(string inputPath, StringEncoding encoding) =>
        RtonDecoder.Decode(File.ReadAllBytes(inputPath), encoding) is { Length: > 0 } decoded
            ? decoded : throw new InvalidDataException(LiarUtil.Core.Strings.RTONDecodingFailed);

    private static byte[] Encode(string inputPath, StringEncoding encoding) =>
        RtonEncoder.Encode(File.ReadAllText(inputPath, Encoding.UTF8), encoding) is { Length: > 0 } encoded
            ? encoded : throw new InvalidDataException(LiarUtil.Core.Strings.RTONEncodingFailed);

    private static byte[] Decrypt(string inputPath, string key) =>
        RtonCrypto.DecryptBytes(File.ReadAllBytes(inputPath), key) is { Length: > 0 } decrypted
            ? decrypted : throw new InvalidDataException(LiarUtil.Core.Strings.RTONDecryptionFailed);

    private static byte[] Encrypt(string inputPath, string key) =>
        RtonCrypto.EncryptBytes(File.ReadAllBytes(inputPath), key) is { Length: > 0 } encrypted
            ? encrypted : throw new InvalidDataException(LiarUtil.Core.Strings.RTONEncryptionFailed);
}

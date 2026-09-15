using System.Buffers.Binary;

namespace LiarUtil.Core.Xnb.Audio;

internal static class WaveFormatParser
{
    private const int BaseHeaderLength = 16;
    private const int ExtendedHeaderLength = 18;
    private const int AdpcmHeaderLength = 22;

    public static IReadOnlyList<AdpcmCoefficient> DefaultAdpcmCoefficients { get; } =
    [
        new(256, 0),
        new(512, -256),
        new(0, 0),
        new(192, 64),
        new(240, 0),
        new(460, -208),
        new(392, -232),
    ];

    public static WaveFormat Parse(ReadOnlySpan<byte> header)
    {
        if (header.Length < BaseHeaderLength)
        {
            throw new XnbException(LiarUtil.Core.Strings.AudioFormatHeaderTruncated);
        }

        var tag = (WaveFormatTag)BinaryPrimitives.ReadUInt16LittleEndian(header);
        var channels = BinaryPrimitives.ReadUInt16LittleEndian(header[2..]);
        var sampleRate = (int)BinaryPrimitives.ReadUInt32LittleEndian(header[4..]);
        var averageBytesPerSecond = (int)BinaryPrimitives.ReadUInt32LittleEndian(header[8..]);
        var blockAlign = BinaryPrimitives.ReadUInt16LittleEndian(header[12..]);
        var bitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(header[14..]);
        if (channels is 0 or > 16)
        {
            throw new XnbException(string.Format(LiarUtil.Core.Strings.AudioChannelCountInvalid0, channels));
        }

        if (sampleRate <= 0)
        {
            throw new XnbException(string.Format(LiarUtil.Core.Strings.AudioSampleRateInvalid0, sampleRate));
        }

        var samplesPerBlock = 0;
        IReadOnlyList<AdpcmCoefficient> coefficients = [];
        if (tag == WaveFormatTag.MsAdpcm && header.Length >= AdpcmHeaderLength)
        {
            samplesPerBlock = BinaryPrimitives.ReadUInt16LittleEndian(header[ExtendedHeaderLength..]);
            coefficients = ParseCoefficients(header);
        }
        else if (tag == WaveFormatTag.ImaAdpcm && header.Length >= ExtendedHeaderLength + 2)
        {
            samplesPerBlock = BinaryPrimitives.ReadUInt16LittleEndian(header[ExtendedHeaderLength..]);
        }

        return new WaveFormat
        {
            Tag = tag,
            Channels = channels,
            SampleRate = sampleRate,
            AverageBytesPerSecond = averageBytesPerSecond,
            BlockAlign = blockAlign > 0 ? blockAlign : Math.Max(channels * bitsPerSample / 8, 1),
            BitsPerSample = bitsPerSample,
            SamplesPerBlock = samplesPerBlock,
            Coefficients = coefficients,
        };
    }

    private static IReadOnlyList<AdpcmCoefficient> ParseCoefficients(ReadOnlySpan<byte> header)
    {
        var count = BinaryPrimitives.ReadUInt16LittleEndian(header[20..]);
        if (count is 0 or > 64 || header.Length < AdpcmHeaderLength + count * 4)
        {
            return DefaultAdpcmCoefficients;
        }

        var coefficients = new AdpcmCoefficient[count];
        for (var index = 0; index < coefficients.Length; index++)
        {
            var offset = AdpcmHeaderLength + index * 4;
            coefficients[index] = new AdpcmCoefficient(
                BinaryPrimitives.ReadInt16LittleEndian(header[offset..]),
                BinaryPrimitives.ReadInt16LittleEndian(header[(offset + 2)..]));
        }

        return coefficients;
    }
}

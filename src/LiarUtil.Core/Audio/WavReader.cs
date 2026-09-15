using System.Buffers.Binary;

namespace LiarUtil.Core.Audio;

public static class WavReader
{
    private const int PcmFormat = 1;
    private const int BitsPerSample = 16;

    public static AudioData Read(ReadOnlySpan<byte> data)
    {
        if (data.Length < 12 || !Matches(data, 0, "RIFF") || !Matches(data, 8, "WAVE"))
        {
            throw new AudioException(LiarUtil.Core.Strings.InputNotRIFFWAVEFile);
        }

        var offset = 12;
        byte[]? format = null;
        byte[]? samples = null;
        while (offset + 8 <= data.Length)
        {
            var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + 4)..]);
            var start = offset + 8;
            if (size < 0 || size > data.Length - start)
            {
                throw new AudioException(LiarUtil.Core.Strings.WAVChunkTruncated);
            }

            if (format is null && Matches(data, offset, "fmt "))
            {
                format = data.Slice(start, size).ToArray();
            }
            else if (samples is null && Matches(data, offset, "data"))
            {
                samples = data.Slice(start, size).ToArray();
            }

            offset = start + size + (size & 1);
        }

        if (format is null || samples is null || format.Length < 16)
        {
            throw new AudioException(LiarUtil.Core.Strings.WAVFileLacksFmtOrDataChunk);
        }

        if (BinaryPrimitives.ReadUInt16LittleEndian(format) != PcmFormat
            || BinaryPrimitives.ReadUInt16LittleEndian(format.AsSpan(14)) != BitsPerSample)
        {
            throw new AudioException(LiarUtil.Core.Strings.InputMust16BitPCMWAV);
        }

        var channels = BinaryPrimitives.ReadUInt16LittleEndian(format.AsSpan(2));
        var sampleRate = (int)BinaryPrimitives.ReadUInt32LittleEndian(format.AsSpan(4));
        if (channels is 0 or > 16 || sampleRate is 0 or > 262143 || samples.Length % (channels * sizeof(short)) != 0)
        {
            throw new AudioException(LiarUtil.Core.Strings.InvalidPCMWAVLayout);
        }

        var values = new short[samples.Length / sizeof(short)];
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = BinaryPrimitives.ReadInt16LittleEndian(samples.AsSpan(index * sizeof(short)));
        }

        return new AudioData
        {
            SampleRate = sampleRate,
            Channels = channels,
            Samples = values,
        };
    }

    private static bool Matches(ReadOnlySpan<byte> data, int offset, string value)
    {
        if (offset + value.Length > data.Length)
        {
            return false;
        }

        for (var index = 0; index < value.Length; index++)
        {
            if (data[offset + index] != value[index])
            {
                return false;
            }
        }

        return true;
    }
}

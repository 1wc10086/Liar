using System.Buffers.Binary;
using LiarUtil.Core.Audio;
using NLayer;

namespace LiarUtil.Core.Sps;

internal static class EaLayer3Codec
{
    private const int FrameSamples = 576;

    private static readonly int[] Versions = [3, -1, 2, 1];

    private static readonly int[][] RateTable =
    [
        [11025, 12000, 8000],
        [],
        [22050, 24000, 16000],
        [44100, 48000, 32000],
    ];

    public static AudioData Decode(ReadOnlySpan<byte> source, EaacHeader header)
    {
        var data = source.ToArray();
        var frames = new List<EaLayer3Frame>();
        var offset = 0;
        while (offset < data.Length)
        {
            var frame = ParseFrame(data, offset);
            frames.Add(frame);
            offset += frame.FrameSize;
        }

        var rebuilt = new List<Mp3Frame>();
        var sampleRate = 0;
        var channels = 0;
        for (var index = 0; index < frames.Count;)
        {
            var first = frames[index];
            EaLayer3Frame? second = first.Mpeg1 && index + 1 < frames.Count ? frames[index + 1] : null;
            if (first.Mpeg1 && second is null)
            {
                second = CloneGranule(first);
            }

            var bytes = RebuildFrame(first, second);
            rebuilt.Add(new Mp3Frame(bytes, ToVersion(first.VersionIndex), first.RateIndex, (MpegChannelMode)first.ChannelMode, first.ModeExtension));
            sampleRate = first.SampleRate;
            channels = first.Channels;
            index += first.Mpeg1 ? 2 : 1;
        }

        if (channels != header.ChannelConfig + 1)
        {
            throw new SpsException(LiarUtil.Core.Strings.EALayer3MPEGDecodingFailed);
        }

        var decoded = new Mp3FrameDecoder().Decode(rebuilt, channels, header.SampleCount + 2304);
        var output = new float[header.SampleCount * channels];
        var copyFrames = Math.Min(header.SampleCount, decoded.Length / channels);
        for (var index = 0; index < copyFrames * channels; index++)
        {
            output[index] = decoded[index] / 32768f;
        }

        PatchPcmFrames(data, frames, output, channels, header.SampleCount);
        return new AudioData
        {
            SampleRate = sampleRate,
            Channels = channels,
            Samples = ToSamples(output),
        };
    }

    private static MpegVersion ToVersion(int versionIndex) => versionIndex switch
    {
        3 => MpegVersion.Version1,
        2 => MpegVersion.Version2,
        _ => MpegVersion.Version25,
    };

    private static void PatchPcmFrames(byte[] data, List<EaLayer3Frame> frames, float[] output, int channels, int sampleCount)
    {
        for (var index = 0; index < frames.Count; index++)
        {
            var frame = frames[index];
            if (frame.PcmSamples == 0)
            {
                continue;
            }

            if (frame.Channels != channels || frame.OffsetMode != 0)
            {
                throw new SpsException(LiarUtil.Core.Strings.UnsupportedEALayer3PCMPatchLayout);
            }

            var destinationFrame = index * FrameSamples;
            for (var sample = 0; sample < frame.PcmSamples && destinationFrame + sample < sampleCount; sample++)
            {
                for (var channel = 0; channel < channels; channel++)
                {
                    var offset = frame.PcmOffset + (sample * channels + channel) * sizeof(short);
                    output[(destinationFrame + sample) * channels + channel] = BinaryPrimitives.ReadInt16BigEndian(data[offset..]) / 32768f;
                }
            }
        }
    }

    private static short[] ToSamples(float[] values)
    {
        var samples = new short[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            samples[index] = WavWriter.FloatToInt16(values[index]);
        }

        return samples;
    }

    private static EaLayer3Frame CloneGranule(EaLayer3Frame source) => new()
    {
        Offset = source.Offset,
        FrameSize = source.FrameSize,
        PreSize = source.PreSize,
        PcmSamples = source.PcmSamples,
        PcmOffset = source.PcmOffset,
        OffsetMode = source.OffsetMode,
        SampleRate = source.SampleRate,
        Channels = source.Channels,
        Mpeg1 = source.Mpeg1,
        VersionIndex = source.VersionIndex,
        RateIndex = source.RateIndex,
        ChannelMode = source.ChannelMode,
        ModeExtension = source.ModeExtension,
        Granule = source.Granule ^ 1,
        DataBit = source.DataBit,
        OtherBits = source.OtherBits,
        Scfsi = [.. source.Scfsi],
        MainBits = [.. source.MainBits],
        OtherBits1 = [.. source.OtherBits1],
        OtherBits2 = [.. source.OtherBits2],
        Data = source.Data,
    };

    private static EaLayer3Frame ParseFrame(byte[] data, int offset)
    {
        if (offset + 2 > data.Length)
        {
            throw new SpsException(LiarUtil.Core.Strings.EALayer3FrameTruncated);
        }

        var first = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        var frameSize = first & 4095;
        var extended = (first & 32768) != 0;
        var preSize = extended ? 6 : 2;
        if (frameSize == 0 || offset + frameSize > data.Length || frameSize < preSize + 1)
        {
            throw new SpsException(LiarUtil.Core.Strings.InvalidEALayer3FrameSize);
        }

        var pcmSamples = 0;
        var commonSizeHint = 0;
        var offsetMode = 0;
        if (extended)
        {
            var extension = BinaryPrimitives.ReadUInt32BigEndian(data[(offset + 2)..]);
            offsetMode = (int)(extension >> 30);
            pcmSamples = (int)((extension >> 10) & 1023);
            commonSizeHint = (int)(extension & 1023);
        }

        var reader = new EaLayer3Reader(data, (offset + preSize) * 8);
        var versionIndex = reader.Read(2);
        var rateIndex = reader.Read(2);
        var channelMode = reader.Read(2);
        var modeExtension = reader.Read(2);
        var granule = reader.Read(1);
        var version = Versions[versionIndex];
        var sampleRate = RateTable[versionIndex].Length > rateIndex ? RateTable[versionIndex][rateIndex] : 0;
        var channels = channelMode == 3 ? 1 : 2;
        var mpeg1 = version == 1;
        if (sampleRate == 0 || version < 0)
        {
            throw new SpsException(LiarUtil.Core.Strings.InvalidEALayer3MPEGParameters);
        }

        var scfsi = new int[channels];
        if (mpeg1 && granule == 1)
        {
            for (var channel = 0; channel < channels; channel++)
            {
                scfsi[channel] = reader.Read(4);
            }
        }

        var mainBits = new int[channels];
        var otherBits1 = new int[channels];
        var otherBits2 = new int[channels];
        var otherBits = mpeg1 ? 15 : 19;
        var dataBits = 0;
        for (var channel = 0; channel < channels; channel++)
        {
            mainBits[channel] = reader.Read(12);
            otherBits1[channel] = reader.Read(32);
            otherBits2[channel] = reader.Read(otherBits);
            dataBits += mainBits[channel];
        }

        var dataBit = reader.Bit - offset * 8;
        var alignment = ((reader.Bit - (offset + preSize) * 8 + dataBits) & 7);
        reader.Skip(dataBits + ((8 - alignment) & 7));
        var commonSize = (reader.Bit - (offset + preSize) * 8) / 8;
        var pcmSize = pcmSamples * channels * sizeof(short);
        if ((commonSizeHint != 0 && commonSizeHint != commonSize) || preSize + commonSize + pcmSize != frameSize)
        {
            throw new SpsException(LiarUtil.Core.Strings.InvalidEALayer3FrameLayout);
        }

        return new EaLayer3Frame
        {
            Offset = offset,
            FrameSize = frameSize,
            PreSize = preSize,
            PcmSamples = pcmSamples,
            PcmOffset = offset + preSize + commonSize,
            OffsetMode = offsetMode,
            SampleRate = sampleRate,
            Channels = channels,
            Mpeg1 = mpeg1,
            VersionIndex = versionIndex,
            RateIndex = rateIndex,
            ChannelMode = channelMode,
            ModeExtension = modeExtension,
            Granule = granule,
            DataBit = dataBit,
            OtherBits = otherBits,
            Scfsi = scfsi,
            MainBits = mainBits,
            OtherBits1 = otherBits1,
            OtherBits2 = otherBits2,
            Data = data[offset..(offset + frameSize)].ToArray(),
        };
    }

    private static byte[] RebuildFrame(EaLayer3Frame first, EaLayer3Frame? second)
    {
        if (first.Mpeg1 && (second is null || first.Granule == second.Granule || first.Channels != second.Channels || first.SampleRate != second.SampleRate))
        {
            throw new SpsException(LiarUtil.Core.Strings.EALayer3GranulePairMismatch);
        }

        var frameSize = (first.Mpeg1 ? 144 * 640000 : 72 * 320000) / first.SampleRate;
        var writer = new EaLayer3BitWriter(frameSize);
        writer.Write(11, 2047);
        writer.Write(2, first.VersionIndex);
        writer.Write(2, 1);
        writer.Write(1, 1);
        writer.Write(4, 0);
        writer.Write(2, first.RateIndex);
        writer.Write(1, 0);
        writer.Write(1, 0);
        writer.Write(2, first.ChannelMode);
        writer.Write(2, first.ModeExtension);
        writer.Write(1, 1);
        writer.Write(1, 1);
        writer.Write(2, 0);
        if (first.Mpeg1)
        {
            writer.Write(9, 0);
            writer.Write(first.Channels == 1 ? 5 : 3, 0);
            for (var channel = 0; channel < second!.Channels; channel++)
            {
                writer.Write(4, second.Scfsi[channel]);
            }
        }
        else
        {
            writer.Write(8, 0);
            writer.Write(first.Channels == 1 ? 1 : 2, 0);
        }

        EaLayer3Frame[] frames = first.Mpeg1 ? [first, second!] : [first];
        foreach (var frame in frames)
        {
            for (var channel = 0; channel < frame.Channels; channel++)
            {
                writer.Write(12, frame.MainBits[channel]);
                writer.Write(32, frame.OtherBits1[channel]);
                writer.Write(frame.OtherBits, frame.OtherBits2[channel]);
            }
        }

        foreach (var frame in frames)
        {
            var reader = new EaLayer3Reader(frame.Data, frame.DataBit);
            for (var channel = 0; channel < frame.Channels; channel++)
            {
                writer.CopyBits(reader, frame.MainBits[channel]);
            }
        }

        return writer.ToFrame(frameSize);
    }
}

using System.Buffers.Binary;
using LiarUtil.Core.Audio;

namespace LiarUtil.Core.Sps;

internal static class SpsContainer
{
    private const int ChunkSamples = 4608;
    private const int ChunkBytesPerChannel = 2736;

    public static byte[] Encode(AudioData audio)
    {
        if (audio.Channels is < 1 or > 16)
        {
            throw new SpsException(LiarUtil.Core.Strings.SPSChannelCountInvalid);
        }

        var samplesPerChannel = audio.FrameCount;
        if (samplesPerChannel == 0)
        {
            throw new SpsException(LiarUtil.Core.Strings.SPSInputHasNoSampleData);
        }

        if (samplesPerChannel > 0x3FFFFFFF || audio.SampleRate > 0x03FFFF)
        {
            throw new SpsException(LiarUtil.Core.Strings.SPSInputOutOfFormatRange);
        }

        var xas = XasCodec.Encode(audio.Samples, samplesPerChannel, audio.Channels);
        var chunkSize = ChunkBytesPerChannel * audio.Channels;
        var capacity = sizeof(uint) * 3 + xas.Length + (xas.Length / chunkSize + 1) * sizeof(uint) * 2 + sizeof(uint);
        var writer = new Core.Binary.BufferWriter(capacity, Core.Binary.ByteOrder.Big);
        writer.WriteUInt32(0x4800000C);
        writer.WriteUInt32((uint)((0x14 << 24) | ((audio.Channels - 1) << 18) | audio.SampleRate));
        writer.WriteUInt32((uint)(samplesPerChannel | 0x40000000));
        var offset = 0;
        var remaining = samplesPerChannel;
        while (offset < xas.Length)
        {
            var size = Math.Min(chunkSize, xas.Length - offset);
            writer.WriteUInt32((uint)(0x44000000 | (size + 8)));
            writer.WriteUInt32((uint)Math.Min(ChunkSamples, remaining));
            writer.WriteBytes(xas.AsSpan(offset, size));
            offset += size;
            remaining -= ChunkSamples;
        }

        writer.WriteUInt32(0x45000004);
        return writer.ToArray();
    }

    public static AudioData Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length < 16)
        {
            throw new SpsException(LiarUtil.Core.Strings.SPSFileHeaderTruncated);
        }

        if (data[0] != 0x48)
        {
            throw new SpsException(LiarUtil.Core.Strings.SPSChunkIdentifierMismatch);
        }

        var header = new EaacHeader(BinaryPrimitives.ReadUInt32BigEndian(data[4..]), BinaryPrimitives.ReadUInt32BigEndian(data[8..]));
        var payload = ReadPayload(data, 12);
        if (header.Codec == 6)
        {
            return EaLayer3Codec.Decode(payload, header);
        }

        if (header.Codec != 4)
        {
            throw new SpsException(string.Format(LiarUtil.Core.Strings.UnsupportedSPSEncoding0, header.Codec));
        }

        var channels = ResolveChannels(header, payload.Length);
        var samples = XasCodec.Decode(payload, header.SampleCount, channels);
        return new AudioData
        {
            SampleRate = header.SampleRate,
            Channels = channels,
            Samples = samples,
        };
    }

    public static int ResolveChannels(EaacHeader header, int payloadLength)
    {
        var bytesPerChannel = ((header.SampleCount + XasConstants.SamplesPerChunk - 1) / XasConstants.SamplesPerChunk) * XasConstants.ChunkSize;
        if (bytesPerChannel == 0 || payloadLength % bytesPerChannel != 0)
        {
            throw new SpsException(LiarUtil.Core.Strings.XASDataLengthDoesNotMatchEAACSample);
        }

        var channels = payloadLength / bytesPerChannel;
        if (channels is 0 or > 16)
        {
            throw new SpsException(LiarUtil.Core.Strings.UnsupportedEAACChannelCount);
        }

        return channels;
    }

    public static byte[] ReadPayload(ReadOnlySpan<byte> data, int blockOffset)
    {
        using var stream = new MemoryStream();
        var offset = blockOffset;
        while (offset + 4 <= data.Length)
        {
            var magic = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
            var identifier = magic >> 24;
            var size = (int)(magic & 0xFFFFFF);
            offset += 4;
            if (identifier == 0x45)
            {
                break;
            }

            if (identifier != 0x44 || size < 8 || size - 4 > data.Length - offset)
            {
                throw new SpsException(LiarUtil.Core.Strings.SPSDataChunkInvalid);
            }

            offset += 4;
            var payload = size - 8;
            if (payload > data.Length - offset)
            {
                throw new SpsException(LiarUtil.Core.Strings.SPSDataChunkTruncated);
            }

            stream.Write(data.Slice(offset, payload));
            offset += payload;
        }

        return stream.ToArray();
    }
}

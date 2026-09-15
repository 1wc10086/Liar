using System.Buffers.Binary;
using LiarUtil.Core.Audio;
using LiarUtil.Core.Sps;

namespace LiarUtil.Core.Snr;

internal static class SnrContainer
{
    public static byte[] Encode(AudioData audio)
    {
        if (audio.Channels is < 1 or > 16)
        {
            throw new SnrException(LiarUtil.Core.Strings.SNRChannelCountInvalid);
        }

        var samplesPerChannel = audio.FrameCount;
        if (samplesPerChannel == 0)
        {
            throw new SnrException(LiarUtil.Core.Strings.SNRInputHasNoSampleData);
        }

        if (samplesPerChannel > 0x1FFFFFFF || audio.SampleRate > 0x03FFFF)
        {
            throw new SnrException(LiarUtil.Core.Strings.SNRInputOutOfFormatRange);
        }

        var xas = XasCodec.Encode(audio.Samples, samplesPerChannel, audio.Channels);
        var writer = new Core.Binary.BufferWriter(16 + xas.Length, Core.Binary.ByteOrder.Big);
        writer.WriteUInt32((uint)((4 << 24) | ((audio.Channels - 1) << 18) | audio.SampleRate));
        writer.WriteUInt32((uint)samplesPerChannel);
        writer.WriteUInt32((uint)(xas.Length + 8));
        writer.WriteUInt32((uint)samplesPerChannel);
        writer.WriteBytes(xas);
        return writer.ToArray();
    }

    public static AudioData Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length < 16)
        {
            throw new SnrException(LiarUtil.Core.Strings.SNRFileHeaderTruncated);
        }

        var header = new EaacHeader(BinaryPrimitives.ReadUInt32BigEndian(data), BinaryPrimitives.ReadUInt32BigEndian(data[4..]));
        if (header.Codec != 4)
        {
            throw new SnrException(LiarUtil.Core.Strings.SNREncodingMismatch);
        }

        var size = (int)BinaryPrimitives.ReadUInt32BigEndian(data[8..]);
        if (size < 8 || size - 8 > data.Length - 16)
        {
            throw new SnrException(LiarUtil.Core.Strings.SNRDataChunkInvalid);
        }

        var payload = data.Slice(16, size - 8);
        var channels = SpsContainer.ResolveChannels(header, payload.Length);
        var samples = XasCodec.Decode(payload, header.SampleCount, channels);
        return new AudioData
        {
            SampleRate = header.SampleRate,
            Channels = channels,
            Samples = samples,
        };
    }
}

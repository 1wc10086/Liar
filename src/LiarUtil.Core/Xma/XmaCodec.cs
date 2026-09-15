using System.Buffers.Binary;
using KevInc.Audio;
using KevInc.Audio.Xma2;
using LiarUtil.Core.Audio;
using LiarUtil.Core.Core.Binary;

namespace LiarUtil.Core.Xma;

public static class XmaCodec
{
    private const ushort Xma2FormatTag = 0x166;
    private const uint BytesPerBlock = 0x800;
    private const byte EncoderVersion = 4;
    private const int FmtChunkSize = 52;

    public static AudioData Decode(byte[] data)
    {
        var chunks = ParseRiff(data);
        var format = data[chunks.FormatOffset..(chunks.FormatOffset + chunks.FormatSize)];
        var bigEndian = BinaryPrimitives.ReadUInt16BigEndian(format) == Xma2FormatTag;
        if (!bigEndian && BinaryPrimitives.ReadUInt16LittleEndian(format) != Xma2FormatTag)
        {
            throw new XmaException(LiarUtil.Core.Strings.InputNotXMA2Audio);
        }

        using var stream = new MemoryStream(data, writable: false);
        using var reader = new BinaryReader(stream);
        Pcm16Audio pcm;
        try
        {
            pcm = Xma2Decoder.Decode(reader, (uint)chunks.FormatOffset, (uint)chunks.DataOffset, chunks.DataSize, bigEndian);
        }
        catch (Exception exception) when (exception is not XmaException)
        {
            throw new XmaException(LiarUtil.Core.Strings.XMA2DecodingFailed, exception);
        }

        return new AudioData
        {
            SampleRate = pcm.SampleRate,
            Channels = pcm.Channels,
            Samples = pcm.Samples,
        };
    }

    public static byte[] DecodeToWav(byte[] data) => WavWriter.Write(Decode(data));

    public static byte[] Encode(byte[] wavData)
    {
        var audio = WavReader.Read(wavData);
        byte[] payload;
        int samplesEncoded;
        try
        {
            payload = Xma2Encoder.Encode(audio.Samples, audio.SampleRate, audio.Channels, out samplesEncoded);
        }
        catch (Exception exception) when (exception is not XmaException)
        {
            throw new XmaException(LiarUtil.Core.Strings.XMA2EncodingFailed, exception);
        }

        return BuildRiff(audio, payload, samplesEncoded);
    }

    private static XmaChunks ParseRiff(byte[] data)
    {
        if (data.Length < 12 || !Matches(data, 0, "RIFF") || !Matches(data, 8, "WAVE"))
        {
            throw new XmaException(LiarUtil.Core.Strings.InputNotRIFFWAVEFile);
        }

        var offset = 12;
        var formatOffset = -1;
        var formatSize = 0;
        var dataOffset = -1;
        var dataSize = 0L;
        while (offset + 8 <= data.Length)
        {
            var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 4));
            var start = offset + 8;
            if (size < 0 || size > data.Length - start)
            {
                throw new XmaException(LiarUtil.Core.Strings.XMAChunkTruncated);
            }

            if (formatOffset < 0 && Matches(data, offset, "fmt "))
            {
                formatOffset = start;
                formatSize = size;
            }
            else if (dataOffset < 0 && Matches(data, offset, "data"))
            {
                dataOffset = start;
                dataSize = size;
            }

            offset = start + size + (size & 1);
        }

        if (formatOffset < 0 || formatSize < FmtChunkSize || dataOffset < 0)
        {
            throw new XmaException(LiarUtil.Core.Strings.XMAFileLacksFmtOrDataChunk);
        }

        return new XmaChunks(formatOffset, formatSize, dataOffset, dataSize);
    }

    private static byte[] BuildRiff(AudioData audio, byte[] payload, int samplesEncoded)
    {
        var dataSize = payload.Length;
        var pad = dataSize & 1;
        var writer = new BufferWriter(12 + 8 + FmtChunkSize + 8 + dataSize + pad, ByteOrder.Little);
        writer.WriteUInt8((byte)'R');
        writer.WriteUInt8((byte)'I');
        writer.WriteUInt8((byte)'F');
        writer.WriteUInt8((byte)'F');
        writer.WriteUInt32((uint)(4 + 8 + FmtChunkSize + 8 + dataSize + pad));
        writer.WriteUInt8((byte)'W');
        writer.WriteUInt8((byte)'A');
        writer.WriteUInt8((byte)'V');
        writer.WriteUInt8((byte)'E');
        writer.WriteUInt8((byte)'f');
        writer.WriteUInt8((byte)'m');
        writer.WriteUInt8((byte)'t');
        writer.WriteUInt8((byte)' ');
        writer.WriteUInt32(FmtChunkSize);
        writer.WriteUInt16(Xma2FormatTag);
        writer.WriteUInt16((ushort)audio.Channels);
        writer.WriteUInt32((uint)audio.SampleRate);
        writer.WriteUInt32((uint)((long)audio.SampleRate * dataSize / Math.Max(1, audio.FrameCount)));
        writer.WriteUInt16(1);
        writer.WriteUInt16(16);
        writer.WriteUInt16(FmtChunkSize - 18);
        writer.WriteUInt16((ushort)audio.Channels);
        writer.WriteUInt32(ChannelMask(audio.Channels));
        writer.WriteUInt32((uint)samplesEncoded);
        writer.WriteUInt32(BytesPerBlock);
        writer.WriteUInt32(0);
        writer.WriteUInt32((uint)samplesEncoded);
        writer.WriteUInt32(0);
        writer.WriteUInt32(0);
        writer.WriteUInt8(0);
        writer.WriteUInt8(EncoderVersion);
        writer.WriteUInt16((ushort)((dataSize + BytesPerBlock - 1) / BytesPerBlock));
        writer.WriteUInt8((byte)'d');
        writer.WriteUInt8((byte)'a');
        writer.WriteUInt8((byte)'t');
        writer.WriteUInt8((byte)'a');
        writer.WriteUInt32((uint)dataSize);
        writer.WriteBytes(payload);
        if (pad != 0)
        {
            writer.WriteUInt8(0);
        }

        return writer.ToArray();
    }

    private static uint ChannelMask(int channels) => channels switch
    {
        1 => 4,
        2 => 3,
        _ => (1u << Math.Min(channels, 18)) - 1,
    };

    private static bool Matches(byte[] data, int offset, string value)
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

    private readonly record struct XmaChunks(int FormatOffset, int FormatSize, int DataOffset, long DataSize);
}

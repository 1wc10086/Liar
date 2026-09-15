using NLayer;

namespace LiarUtil.Core.Audio;

internal sealed class Mp3Frame(byte[] data, MpegVersion version, int sampleRateIndex, MpegChannelMode channelMode, int channelModeExtension) : IMpegFrame
{
    private const int HeaderBits = 32;
    private const int FreeBitRateIndex = 0;

    private int _offset;
    private int _bitsRead;
    private ulong _bitBucket;

    public int SampleRate { get; } = ResolveSampleRate(version, sampleRateIndex);

    public int SampleRateIndex { get; } = sampleRateIndex;

    public int FrameLength => data.Length;

    public int BitRate => 0;

    public MpegVersion Version { get; } = version;

    public MpegLayer Layer => MpegLayer.LayerIII;

    public MpegChannelMode ChannelMode { get; } = channelMode;

    public int ChannelModeExtension { get; } = channelModeExtension;

    public int SampleCount => Version == MpegVersion.Version1 ? 1152 : 576;

    public int BitRateIndex => FreeBitRateIndex;

    public bool IsCopyrighted => false;

    public bool HasCrc => false;

    public bool IsCorrupted => false;

    private static int ResolveSampleRate(MpegVersion version, int sampleRateIndex) => version switch
    {
        MpegVersion.Version1 => (int)(sampleRateIndex switch
        {
            0 => 44100,
            1 => 48000,
            2 => 32000,
            _ => 0,
        }),
        MpegVersion.Version2 => (int)(sampleRateIndex switch
        {
            0 => 22050,
            1 => 24000,
            2 => 16000,
            _ => 0,
        }),
        _ => (int)(sampleRateIndex switch
        {
            0 => 11025,
            1 => 12000,
            2 => 8000,
            _ => 0,
        }),
    };

    public void Reset()
    {
        _offset = HeaderBits / 8;
        _bitBucket = 0UL;
        _bitsRead = 0;
    }

    public int ReadBits(int bitCount)
    {
        if (bitCount is < 1 or > 32)
        {
            throw new AudioException(LiarUtil.Core.Strings.InvalidMP3BitReadLength);
        }

        while (_bitsRead < bitCount)
        {
            if (_offset >= data.Length)
            {
                throw new AudioException(LiarUtil.Core.Strings.MP3FrameDataInsufficient);
            }

            _bitBucket <<= 8;
            _bitBucket |= data[_offset++];
            _bitsRead += 8;
        }

        var value = (int)((_bitBucket >> (_bitsRead - bitCount)) & ((1UL << bitCount) - 1));
        _bitsRead -= bitCount;
        return value;
    }
}

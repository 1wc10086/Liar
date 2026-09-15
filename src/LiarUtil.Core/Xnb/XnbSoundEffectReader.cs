using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Xnb.Audio;

namespace LiarUtil.Core.Xnb;

internal static class XnbSoundEffectReader
{
    private const int MinimumHeaderLength = 16;
    private const int TrailerLength = 12;

    public static SoundEffectData Read(byte[] data, int offset, int length)
    {
        var reader = new BufferReader(data, offset, length) { ErrorFactory = XnbErrors.Throw };
        var headerLength = reader.ReadInt32();
        if (headerLength < MinimumHeaderLength || headerLength > reader.Remaining - sizeof(int) - TrailerLength)
        {
            throw new XnbException(string.Format(LiarUtil.Core.Strings.SoundEffectFormatHeaderLengthInvalid0, headerLength));
        }

        var header = reader.ReadBytes(headerLength);
        var dataLength = reader.ReadInt32();
        if (dataLength < 0 || dataLength > reader.Remaining - TrailerLength)
        {
            throw new XnbException(string.Format(LiarUtil.Core.Strings.SoundEffectSampleDataLengthInvalid0, dataLength));
        }

        var samples = reader.ReadBytes(dataLength);
        var loopStart = reader.ReadInt32();
        var loopLength = reader.ReadInt32();
        var duration = reader.ReadInt32();
        return new SoundEffectData
        {
            Format = WaveFormatParser.Parse(header),
            Data = samples,
            LoopStart = loopStart,
            LoopLength = loopLength,
            DurationMilliseconds = duration,
        };
    }
}

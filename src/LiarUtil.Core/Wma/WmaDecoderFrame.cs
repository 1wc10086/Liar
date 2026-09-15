namespace LiarUtil.Core.Wma;

internal sealed partial class WmaDecoder
{
    private readonly short[] _frameSamples;

    private readonly float[] _expPower;

    private bool DecodeFrame(ref BitReader reader)
    {
        _blockPos = 0;
        while (true)
        {
            var result = DecodeBlock(ref reader);
            if (result < 0)
            {
                return false;
            }

            if (result != 0)
            {
                break;
            }
        }

        var n = _frameLen;
        var channels = _info.Channels;
        for (var ch = 0; ch < channels; ch++)
        {
            var frameOut = _frameOut[ch];
            for (var i = 0; i < n; i++)
            {
                var value = (int)Math.Round(frameOut[i], MidpointRounding.ToEven);
                if (value > 32767)
                {
                    value = 32767;
                }
                else if (value < -32768)
                {
                    value = -32768;
                }

                _frameSamples[(i * channels) + ch] = (short)value;
            }

            for (var i = 0; i < n; i++)
            {
                frameOut[i] = frameOut[i + n];
                frameOut[i + n] = 0f;
            }
        }

        return true;
    }

    internal void DecodeSuperframe(ReadOnlySpan<byte> buffer, List<short> output)
    {
        if (buffer.Length == 0)
        {
            _lastSuperframeLen = 0;
            return;
        }

        var reader = new BitReader(buffer);

        if (!_useBitReservoir)
        {
            if (DecodeFrame(ref reader))
            {
                output.AddRange(_frameSamples);
            }

            return;
        }

        reader.Read(4);
        var frameCount = reader.Read(4) - 1;
        var bitOffset = reader.Read(_byteOffsetBits + 3);

        if (_lastSuperframeLen > 0)
        {
            if (_lastSuperframeLen + ((bitOffset + 7) >> 3) > MaxCodedSuperframeSize)
            {
                _lastSuperframeLen = 0;
                return;
            }

            var write = _lastSuperframeLen;
            var remaining = bitOffset;
            while (remaining > 0)
            {
                _lastSuperframe[write++] = (byte)reader.Read(8);
                remaining -= 8;
            }

            if (remaining > 0)
            {
                _lastSuperframe[write] = (byte)(reader.Read(remaining) << (8 - remaining));
            }

            var previous = new BitReader(_lastSuperframe);
            if (_lastBitOffset > 0)
            {
                previous.Skip(_lastBitOffset);
            }

            if (!DecodeFrame(ref previous))
            {
                _lastSuperframeLen = 0;
                return;
            }

            output.AddRange(_frameSamples);
        }

        var position = bitOffset + 4 + 4 + _byteOffsetBits + 3;
        var current = new BitReader(buffer[(position >> 3)..]);
        var skip = position & 7;
        if (skip > 0)
        {
            current.Skip(skip);
        }

        _resetBlockLengths = true;
        for (var i = 0; i < frameCount; i++)
        {
            if (!DecodeFrame(ref current))
            {
                _lastSuperframeLen = 0;
                return;
            }

            output.AddRange(_frameSamples);
        }

        position = current.Position + ((bitOffset + 4 + 4 + _byteOffsetBits + 3) & ~7);
        _lastBitOffset = position & 7;
        position >>= 3;
        var length = buffer.Length - position;
        if (length > MaxCodedSuperframeSize || length < 0)
        {
            _lastSuperframeLen = 0;
            return;
        }

        _lastSuperframeLen = length;
        buffer.Slice(position, length).CopyTo(_lastSuperframe);
    }
}

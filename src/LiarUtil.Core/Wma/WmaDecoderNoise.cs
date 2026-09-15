namespace LiarUtil.Core.Wma;

internal sealed partial class WmaDecoder
{
    private void Denormalize(int bsize, int totalGain, float mdctNorm, Span<int> nbCoefs)
    {
        for (var ch = 0; ch < _info.Channels; ch++)
        {
            if (_channelCoded[ch] == 0)
            {
                continue;
            }

            var coefs1 = _coefs1[ch];
            var exponents = _exponents[ch];
            var coefs = _coefs[ch];
            var mult = (float)(Math.Pow(10, totalGain * 0.05) / _maxExponent[ch]);
            mult *= mdctNorm;
            var cursor = 0;
            var maxExponent = _maxExponent[ch];

            if (_useNoiseCoding)
            {
                var mult1 = mult;
                for (var i = 0; i < _coefsStart; i++)
                {
                    coefs[cursor] = _noiseTable[_noiseIndex] * exponents[cursor] * mult1;
                    _noiseIndex = (_noiseIndex + 1) & (NoiseTabSize - 1);
                    cursor++;
                }

                var highCount = _exponentHighSizes[bsize];
                var expCursor = _highBandStart[bsize] - _coefsStart;
                var lastHighBand = 0;
                var expPower = _expPower;

                for (var j = 0; j < highCount; j++)
                {
                    var n = _exponentHighBands[bsize][j];
                    if (_highBandCoded[ch][j] != 0)
                    {
                        var e2 = 0f;
                        for (var i = 0; i < n; i++)
                        {
                            var v = exponents[expCursor + i];
                            e2 += v * v;
                        }

                        expPower[j] = e2 / n;
                        lastHighBand = j;
                    }

                    expCursor += n;
                }

                for (var j = -1; j < highCount; j++)
                {
                    var n = j < 0 ? _highBandStart[bsize] - _coefsStart : _exponentHighBands[bsize][j];
                    if (j >= 0 && _highBandCoded[ch][j] != 0)
                    {
                        var scale = (float)Math.Sqrt(expPower[j] / expPower[lastHighBand]);
                        scale *= (float)Math.Pow(10, _highBandValues[ch][j] * 0.05);
                        scale = scale / (maxExponent * _noiseMult);
                        scale *= mdctNorm;
                        for (var i = 0; i < n; i++)
                        {
                            var noise = _noiseTable[_noiseIndex];
                            _noiseIndex = (_noiseIndex + 1) & (NoiseTabSize - 1);
                            coefs[cursor] = exponents[cursor] * noise * scale;
                            cursor++;
                        }
                    }
                    else
                    {
                        for (var i = 0; i < n; i++)
                        {
                            var noise = _noiseTable[_noiseIndex];
                            _noiseIndex = (_noiseIndex + 1) & (NoiseTabSize - 1);
                            coefs[cursor] = (coefs1[cursor] + noise) * exponents[cursor] * mult;
                            cursor++;
                        }
                    }
                }

                var tail = _blockLen - _coefsEnd[bsize];
                var tailMult = mult * exponents[_coefsEnd[bsize] - _coefsStart - 1];
                for (var i = 0; i < tail; i++)
                {
                    coefs[cursor] = _noiseTable[_noiseIndex] * tailMult;
                    _noiseIndex = (_noiseIndex + 1) & (NoiseTabSize - 1);
                    cursor++;
                }
            }
            else
            {
                for (var i = 0; i < _coefsStart; i++)
                {
                    coefs[cursor++] = 0f;
                }

                var count = nbCoefs[ch];
                for (var i = 0; i < count; i++)
                {
                    coefs[cursor++] = coefs1[i] * exponents[i] * mult;
                }

                var tail = _blockLen - _coefsEnd[bsize];
                for (var i = 0; i < tail; i++)
                {
                    coefs[cursor++] = 0f;
                }
            }

        }

        if (_msStereo && _channelCoded[1] != 0)
        {
            if (_channelCoded[0] == 0)
            {
                _coefs[0].AsSpan(0, _blockLen).Clear();
                _channelCoded[0] = 1;
            }

            var left = _coefs[0];
            var right = _coefs[1];
            for (var i = 0; i < _blockLen; i++)
            {
                var a = left[i];
                var b = right[i];
                left[i] = a + b;
                right[i] = a - b;
            }
        }
    }

    private void BuildWindow(int bsize)
    {
        var blockLen = _blockLen;
        var prevLen = 1 << _prevBlockLenBits;
        var nextLen = 1 << _nextBlockLenBits;
        var window = _window;

        var w = blockLen;
        if (blockLen <= nextLen)
        {
            for (var i = 0; i < blockLen; i++)
            {
                window[w++] = _windows[bsize][i];
            }
        }
        else
        {
            var n = (blockLen / 2) - (nextLen / 2);
            for (var i = 0; i < n; i++)
            {
                window[w++] = 1f;
            }

            var prevWindow = _windows[_frameLenBits - _nextBlockLenBits];
            for (var i = 0; i < nextLen; i++)
            {
                window[w++] = prevWindow[i];
            }

            for (var i = 0; i < n; i++)
            {
                window[w++] = 0f;
            }
        }

        w = blockLen;
        if (blockLen <= prevLen)
        {
            for (var i = 0; i < blockLen; i++)
            {
                window[--w] = _windows[bsize][i];
            }
        }
        else
        {
            var n = (blockLen / 2) - (prevLen / 2);
            for (var i = 0; i < n; i++)
            {
                window[--w] = 1f;
            }

            var prevWindow = _windows[_frameLenBits - _prevBlockLenBits];
            for (var i = 0; i < prevLen; i++)
            {
                window[--w] = prevWindow[i];
            }

            for (var i = 0; i < n; i++)
            {
                window[--w] = 0f;
            }
        }
    }
}

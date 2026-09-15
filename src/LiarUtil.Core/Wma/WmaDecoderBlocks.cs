namespace LiarUtil.Core.Wma;

internal sealed partial class WmaDecoder
{
    private void Interpolate(float[] scale, int oldSize, int newSize)
    {
        if (newSize > oldSize)
        {
            var increment = newSize / oldSize;
            var j = newSize;
            for (var i = oldSize - 1; i >= 0; i--)
            {
                var value = scale[i];
                var k = increment;
                do
                {
                    scale[--j] = value;
                }
                while (--k != 0);
            }
        }
        else if (newSize < oldSize)
        {
            var j = 0;
            var increment = oldSize / newSize;
            for (var i = 0; i < newSize; i++)
            {
                scale[i] = scale[j];
                j += increment;
            }
        }
    }

    private bool DecodeExpVlc(ref BitReader reader, int ch)
    {
        var bsize = _frameLenBits - _blockLenBits;
        var bands = _exponentBands[bsize];
        var bandIndex = 0;
        var exponents = _exponents[ch];
        var q = 0;
        var qEnd = _blockLen;
        var maxScale = 0f;

        if (_info.Version == 1)
        {
            var lastExp = reader.Read(5) + 10;
            var v = (float)Math.Pow(10, lastExp * (1.0 / 16.0));
            maxScale = v;
            var n = bands[bandIndex++];
            for (var i = 0; i < n; i++)
            {
                exponents[q++] = v;
            }
        }

        var exp = 36;
        while (q < qEnd)
        {
            var code = _expVlc!.Decode(ref reader);
            if (code < 0)
            {
                return false;
            }

            exp += code - 60;
            var v = (float)Math.Pow(10, exp * (1.0 / 16.0));
            if (v > maxScale)
            {
                maxScale = v;
            }

            var n = bands[bandIndex++];
            for (var i = 0; i < n; i++)
            {
                exponents[q++] = v;
            }
        }

        _maxExponent[ch] = maxScale;
        return true;
    }

    private void DecodeExpLsp(ref BitReader reader, int ch)
    {
        Span<float> lsp = stackalloc float[10];
        for (var i = 0; i < 10; i++)
        {
            var val = i == 0 || i >= 8 ? reader.Read(3) : reader.Read(4);
            lsp[i] = WmaBandTables.LspCodebook[i][val];
        }

        LspToCurve(ch, lsp);
    }

    private void LspToCurve(int ch, ReadOnlySpan<float> lsp)
    {
        var n = _blockLen;
        var valMax = 0f;
        var outTable = _exponents[ch];

        for (var i = 0; i < n; i++)
        {
            var p = 0.5f;
            var q = 0.5f;
            var w = _lspCosTable[i];
            for (var j = 1; j < 10; j += 2)
            {
                q *= w - lsp[j - 1];
                p *= w - lsp[j];
            }

            p *= p * (2.0f - w);
            q *= q * (2.0f + w);
            var v = PowM1_4(p + q);
            if (v > valMax)
            {
                valMax = v;
            }

            outTable[i] = v;
        }

        _maxExponent[ch] = valMax;
    }

    private int DecodeBlock(ref BitReader reader)
    {
        if (_useVariableBlockLen)
        {
            var n = AvLog2((uint)(_nbBlockSizes - 1)) + 1;
            if (_resetBlockLengths)
            {
                _resetBlockLengths = false;
                var v = reader.Read(n);
                if (v >= _nbBlockSizes)
                {
                    return -1;
                }

                _prevBlockLenBits = _frameLenBits - v;
                v = reader.Read(n);
                if (v >= _nbBlockSizes)
                {
                    return -1;
                }

                _blockLenBits = _frameLenBits - v;
            }
            else
            {
                _prevBlockLenBits = _blockLenBits;
                _blockLenBits = _nextBlockLenBits;
            }

            var next = reader.Read(n);
            if (next >= _nbBlockSizes)
            {
                return -1;
            }

            _nextBlockLenBits = _frameLenBits - next;
        }
        else
        {
            _nextBlockLenBits = _frameLenBits;
            _prevBlockLenBits = _frameLenBits;
            _blockLenBits = _frameLenBits;
        }

        _blockLen = 1 << _blockLenBits;
        if (_blockPos + _blockLen > _frameLen)
        {
            return -1;
        }

        if (_info.Channels == 2)
        {
            _msStereo = reader.Read(1) != 0;
        }

        var anyCoded = 0;
        for (var ch = 0; ch < _info.Channels; ch++)
        {
            var coded = reader.Read(1);
            _channelCoded[ch] = coded;
            anyCoded |= coded;
        }

        if (anyCoded == 0)
        {
            _blockPos += _blockLen;
            return _blockPos >= _frameLen ? 1 : 0;
        }

        var bsize = _frameLenBits - _blockLenBits;

        var totalGain = 1;
        while (true)
        {
            var a = reader.Read(7);
            totalGain += a;
            if (a != 127)
            {
                break;
            }
        }

        var coefBits = totalGain switch
        {
            < 15 => 13,
            < 32 => 12,
            < 40 => 11,
            < 45 => 10,
            _ => 9,
        };

        var baseCoefs = _coefsEnd[bsize] - _coefsStart;
        var nbCoefs = new int[MaxChannels];
        for (var ch = 0; ch < _info.Channels; ch++)
        {
            nbCoefs[ch] = baseCoefs;
        }

        if (_useNoiseCoding)
        {
            for (var ch = 0; ch < _info.Channels; ch++)
            {
                if (_channelCoded[ch] == 0)
                {
                    continue;
                }

                var count = _exponentHighSizes[bsize];
                for (var i = 0; i < count; i++)
                {
                    var coded = reader.Read(1);
                    _highBandCoded[ch][i] = coded;
                    if (coded != 0)
                    {
                        nbCoefs[ch] -= _exponentHighBands[bsize][i];
                    }
                }
            }

            for (var ch = 0; ch < _info.Channels; ch++)
            {
                if (_channelCoded[ch] == 0)
                {
                    continue;
                }

                var count = _exponentHighSizes[bsize];
                var hasValue = false;
                var val = 0;
                for (var i = 0; i < count; i++)
                {
                    if (_highBandCoded[ch][i] == 0)
                    {
                        continue;
                    }

                    if (!hasValue)
                    {
                        val = reader.Read(7) - 19;
                        hasValue = true;
                    }
                    else
                    {
                        var code = _hgainVlc!.Decode(ref reader);
                        if (code < 0)
                        {
                            return -1;
                        }

                        val += code - 18;
                    }

                    _highBandValues[ch][i] = val;
                }
            }
        }

        var parseExponents = true;
        if (_blockLenBits != _frameLenBits)
        {
            parseExponents = reader.Read(1) != 0;
        }

        if (parseExponents)
        {
            for (var ch = 0; ch < _info.Channels; ch++)
            {
                if (_channelCoded[ch] == 0)
                {
                    continue;
                }

                if (_useExpVlc)
                {
                    if (!DecodeExpVlc(ref reader, ch))
                    {
                        return -1;
                    }
                }
                else
                {
                    DecodeExpLsp(ref reader, ch);
                }
            }
        }
        else
        {
            for (var ch = 0; ch < _info.Channels; ch++)
            {
                if (_channelCoded[ch] != 0)
                {
                    Interpolate(_exponents[ch], 1 << _prevBlockLenBits, _blockLen);
                }
            }
        }

        if (!DecodeCoefficients(ref reader, bsize, coefBits, nbCoefs))
        {
            return -1;
        }

        var n4 = _blockLen / 2;
        var mdctNorm = 1.0f / n4;
        if (_info.Version == 1)
        {
            mdctNorm = (float)(mdctNorm * Math.Sqrt(n4));
        }

        Denormalize(bsize, totalGain, mdctNorm, nbCoefs);
        BuildWindow(bsize);

        for (var ch = 0; ch < _info.Channels; ch++)
        {
            if (_channelCoded[ch] == 0)
            {
                continue;
            }

            var n = _blockLen;
            var half = _blockLen / 2;
            _mdct[bsize].Imdct(_coefs[ch], _blockOut);
            var frameOut = _frameOut[ch];
            var index = (_frameLen / 2) + _blockPos - half;
            for (var i = 0; i < n * 2; i++)
            {
                frameOut[index + i] += _blockOut[i] * _window[i];
            }

            if (_msStereo && _channelCoded[1] == 0)
            {
                var other = _frameOut[1];
                for (var i = 0; i < n * 2; i++)
                {
                    other[index + i] += _blockOut[i] * _window[i];
                }
            }
        }

        _blockPos += _blockLen;
        return _blockPos >= _frameLen ? 1 : 0;
    }

    private bool DecodeCoefficients(ref BitReader reader, int bsize, int coefBits, Span<int> nbCoefs)
    {
        for (var ch = 0; ch < _info.Channels; ch++)
        {
            if (_channelCoded[ch] == 0)
            {
                continue;
            }

            var slot = ch == 1 && _msStereo ? 1 : 0;
            var vlc = _coefVlc[slot];
            var runTable = _runTable[slot];
            var levelTable = _levelTable[slot];
            var coefs = _coefs1[ch];
            var limit = nbCoefs[ch];
            var position = 0;

            coefs.AsSpan(0, _blockLen).Clear();

            while (true)
            {
                var code = vlc.Decode(ref reader);
                if (code < 0)
                {
                    return false;
                }

                if (code == 1)
                {
                    break;
                }

                int run;
                int level;
                if (code == 0)
                {
                    level = reader.Read(coefBits);
                    run = reader.Read(_frameLenBits);
                }
                else
                {
                    run = runTable[code];
                    level = levelTable[code];
                }

                if (reader.Read(1) == 0)
                {
                    level = -level;
                }

                position += run;
                if (position >= limit)
                {
                    return false;
                }

                coefs[position++] = (short)level;
                if (position >= limit)
                {
                    break;
                }
            }

            if (_info.Version == 1 && _info.Channels >= 2)
            {
                reader.Align();
            }
        }

        return true;
    }

}

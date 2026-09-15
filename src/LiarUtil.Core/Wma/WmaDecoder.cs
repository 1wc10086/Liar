using System.Numerics;

namespace LiarUtil.Core.Wma;

internal sealed partial class WmaDecoder
{
    private const int BlockMinBits = 7;
    private const int BlockMaxSize = 1 << 11;
    private const int BlockNbSizes = 5;
    private const int HighBandMaxSize = 16;
    private const int MaxCodedSuperframeSize = 4096;
    private const int MaxChannels = 2;
    private const int NoiseTabSize = 8192;
    private const int LspPowBits = 7;

    private readonly AsfInfo _info;
    private readonly int _frameLenBits;
    private readonly int _frameLen;
    private readonly int _nbBlockSizes;
    private readonly bool _useExpVlc;
    private readonly bool _useBitReservoir;
    private readonly bool _useVariableBlockLen;
    private readonly bool _useNoiseCoding;
    private readonly int _byteOffsetBits;
    private readonly int _coefsStart;
    private readonly float _noiseMult;
    private readonly int[] _exponentSizes = new int[BlockNbSizes];
    private readonly int[][] _exponentBands = new int[BlockNbSizes][];
    private readonly int[] _coefsEnd = new int[BlockNbSizes];
    private readonly int[] _highBandStart = new int[BlockNbSizes];
    private readonly int[] _exponentHighSizes = new int[BlockNbSizes];
    private readonly int[][] _exponentHighBands = new int[BlockNbSizes][];
    private readonly WmaMdct[] _mdct = new WmaMdct[BlockNbSizes];
    private readonly float[][] _windows = new float[BlockNbSizes][];
    private readonly float[] _noiseTable = new float[NoiseTabSize];
    private readonly float[] _lspCosTable;
    private readonly float[] _lspPowETable = new float[256];
    private readonly float[] _lspPowM1 = new float[1 << LspPowBits];
    private readonly float[] _lspPowM2 = new float[1 << LspPowBits];
    private readonly VlcTable? _expVlc;
    private readonly VlcTable? _hgainVlc;
    private readonly VlcTable[] _coefVlc = new VlcTable[2];
    private readonly ushort[][] _runTable = new ushort[2][];
    private readonly ushort[][] _levelTable = new ushort[2][];
    private readonly short[][] _coefs1 = new short[MaxChannels][];
    private readonly float[][] _coefs = new float[MaxChannels][];
    private readonly float[][] _exponents = new float[MaxChannels][];
    private readonly float[] _maxExponent = new float[MaxChannels];
    private readonly int[][] _highBandCoded = new int[MaxChannels][];
    private readonly int[][] _highBandValues = new int[MaxChannels][];
    private readonly int[] _channelCoded = new int[MaxChannels];
    private readonly float[][] _frameOut = new float[MaxChannels][];
    private readonly byte[] _lastSuperframe = new byte[MaxCodedSuperframeSize + 4];
    private readonly float[] _window = new float[BlockMaxSize * 2];
    private readonly float[] _blockOut = new float[BlockMaxSize * 2];

    private int _noiseIndex;
    private bool _msStereo;
    private bool _resetBlockLengths = true;
    private int _blockLenBits;
    private int _nextBlockLenBits;
    private int _prevBlockLenBits;
    private int _blockLen;
    private int _blockPos;
    private int _lastBitOffset;
    private int _lastSuperframeLen;

    internal WmaDecoder(AsfInfo info)
    {
        _info = info;

        var flags2 = 0;
        var extra = info.ExtraData;
        if (info.Version == 1 && extra.Length >= 4)
        {
            flags2 = extra[2] | (extra[3] << 8);
        }
        else if (info.Version == 2 && extra.Length >= 6)
        {
            flags2 = extra[4] | (extra[5] << 8);
        }

        _useExpVlc = (flags2 & 0x0001) != 0;
        _useBitReservoir = (flags2 & 0x0002) != 0;
        _useVariableBlockLen = (flags2 & 0x0004) != 0;

        var sampleRate = info.SampleRate;
        if (sampleRate <= 16000)
        {
            _frameLenBits = 9;
        }
        else if (sampleRate <= 22050 || (sampleRate <= 32000 && info.Version == 1))
        {
            _frameLenBits = 10;
        }
        else
        {
            _frameLenBits = 11;
        }

        _frameLen = 1 << _frameLenBits;

        if (_useVariableBlockLen)
        {
            var nb = ((flags2 >> 3) & 3) + 1;
            if (info.BitRate / info.Channels >= 32000)
            {
                nb += 2;
            }

            var nbMax = _frameLenBits - BlockMinBits;
            if (nb > nbMax)
            {
                nb = nbMax;
            }

            _nbBlockSizes = nb + 1;
        }
        else
        {
            _nbBlockSizes = 1;
        }

        var highFreq = (float)(sampleRate * 0.5);
        var sampleRate1 = sampleRate;
        if (info.Version == 2)
        {
            Span<int> limits = [44100, 22050, 16000, 11025, 8000];
            foreach (var limit in limits)
            {
                if (sampleRate1 >= limit)
                {
                    sampleRate1 = limit;
                    break;
                }
            }
        }

        var bps = (float)info.BitRate / (info.Channels * sampleRate);
        _byteOffsetBits = AvLog2((uint)(bps * _frameLen / 8.0)) + 2;

        var bps1 = bps;
        if (info.Channels == 2)
        {
            bps1 = (float)(bps * 1.6);
        }

        _useNoiseCoding = true;
        if (sampleRate1 == 44100)
        {
            if (bps1 >= 0.61f)
            {
                _useNoiseCoding = false;
            }
            else
            {
                highFreq = (float)(highFreq * 0.4);
            }
        }
        else if (sampleRate1 == 22050)
        {
            if (bps1 >= 1.16f)
            {
                _useNoiseCoding = false;
            }
            else if (bps1 >= 0.72f)
            {
                highFreq = (float)(highFreq * 0.7);
            }
            else
            {
                highFreq = (float)(highFreq * 0.6);
            }
        }
        else if (sampleRate1 == 16000)
        {
            highFreq = (float)(highFreq * (bps > 0.5f ? 0.5 : 0.3));
        }
        else if (sampleRate1 == 11025)
        {
            highFreq = (float)(highFreq * 0.7);
        }
        else if (sampleRate1 == 8000)
        {
            if (bps <= 0.625f)
            {
                highFreq = (float)(highFreq * 0.5);
            }
            else if (bps > 0.75f)
            {
                _useNoiseCoding = false;
            }
            else
            {
                highFreq = (float)(highFreq * 0.65);
            }
        }
        else
        {
            if (bps >= 0.8f)
            {
                highFreq = (float)(highFreq * 0.75);
            }
            else if (bps >= 0.6f)
            {
                highFreq = (float)(highFreq * 0.6);
            }
            else
            {
                highFreq = (float)(highFreq * 0.5);
            }
        }

        _coefsStart = info.Version == 1 ? 3 : 0;
        BuildBands(sampleRate, highFreq);

        for (var i = 0; i < _nbBlockSizes; i++)
        {
            _mdct[i] = new WmaMdct(_frameLenBits - i + 1);
            var n = 1 << (_frameLenBits - i);
            var window = new float[n];
            var alpha = Math.PI / (2.0 * n);
            for (var j = 0; j < n; j++)
            {
                window[n - j - 1] = (float)Math.Sin((j + 0.5) * alpha);
            }

            _windows[i] = window;
        }

        for (var ch = 0; ch < MaxChannels; ch++)
        {
            _coefs1[ch] = new short[BlockMaxSize];
            _coefs[ch] = new float[BlockMaxSize];
            _exponents[ch] = new float[BlockMaxSize];
            _highBandCoded[ch] = new int[HighBandMaxSize];
            _highBandValues[ch] = new int[HighBandMaxSize];
            _frameOut[ch] = new float[_frameLen * 2];
        }

        _lspCosTable = new float[_frameLen];
        _frameSamples = new short[_frameLen * info.Channels];
        _expPower = new float[HighBandMaxSize];

        if (_useNoiseCoding)
        {
            _noiseMult = _useExpVlc ? 0.02f : 0.04f;
            var seed = 1u;
            var norm = (float)((1.0 / 2147483648.0) * Math.Sqrt(3) * _noiseMult);
            for (var i = 0; i < NoiseTabSize; i++)
            {
                seed = (seed * 314159) + 1;
                _noiseTable[i] = (int)seed * norm;
            }

            _hgainVlc = VlcTable.Build(WmaScaleTables.HgainCodes, WmaScaleTables.HgainBits);
        }

        if (_useExpVlc)
        {
            _expVlc = VlcTable.Build(WmaScaleTables.ScaleCodes, WmaScaleTables.ScaleBits);
        }
        else
        {
            InitLsp();
        }

        var coefVlcTable = 2;
        if (sampleRate >= 32000)
        {
            if (bps1 < 0.72f)
            {
                coefVlcTable = 0;
            }
            else if (bps1 < 1.16f)
            {
                coefVlcTable = 1;
            }
        }

        InitCoefVlc(coefVlcTable * 2, 0);
        InitCoefVlc((coefVlcTable * 2) + 1, 1);

        _blockLenBits = _frameLenBits;
        _nextBlockLenBits = _frameLenBits;
        _prevBlockLenBits = _frameLenBits;
        _blockLen = _frameLen;
    }

    private static int AvLog2(uint value)
    {
        var n = 0;
        if ((value & 0xffff0000) != 0)
        {
            value >>= 16;
            n += 16;
        }

        if ((value & 0xff00) != 0)
        {
            value >>= 8;
            n += 8;
        }

        return n + BitOperations.Log2(value | 1);
    }

    private void BuildBands(int sampleRate, float highFreq)
    {
        for (var k = 0; k < _nbBlockSizes; k++)
        {
            var blockLen = _frameLen >> k;
            _exponentBands[k] = new int[25];
            _exponentHighBands[k] = new int[HighBandMaxSize];

            if (_info.Version == 1)
            {
                var lpos = 0;
                var i = 0;
                while (i < 25)
                {
                    var a = WmaBandTables.CriticalFreqs[i];
                    var position = ((blockLen * 2 * a) + (sampleRate >> 1)) / sampleRate;
                    if (position > blockLen)
                    {
                        position = blockLen;
                    }

                    _exponentBands[k][i] = position - lpos;
                    if (position >= blockLen)
                    {
                        i++;
                        break;
                    }

                    lpos = position;
                    i++;
                }

                _exponentSizes[k] = i;
            }
            else
            {
                byte[]? table = null;
                var index = _frameLenBits - BlockMinBits - k;
                if (index < 3)
                {
                    if (sampleRate >= 44100)
                    {
                        table = WmaBandTables.ExponentBand44100[index];
                    }
                    else if (sampleRate >= 32000)
                    {
                        table = WmaBandTables.ExponentBand32000[index];
                    }
                    else if (sampleRate >= 22050)
                    {
                        table = WmaBandTables.ExponentBand22050[index];
                    }
                }

                if (table is not null)
                {
                    var n = table[0];
                    for (var i = 0; i < n; i++)
                    {
                        _exponentBands[k][i] = table[i + 1];
                    }

                    _exponentSizes[k] = n;
                }
                else
                {
                    var j = 0;
                    var lpos = 0;
                    for (var i = 0; i < 25; i++)
                    {
                        var a = WmaBandTables.CriticalFreqs[i];
                        var position = ((blockLen * 2 * a) + (sampleRate << 1)) / (4 * sampleRate);
                        position <<= 2;
                        if (position > blockLen)
                        {
                            position = blockLen;
                        }

                        if (position > lpos)
                        {
                            _exponentBands[k][j++] = position - lpos;
                        }

                        if (position >= blockLen)
                        {
                            break;
                        }

                        lpos = position;
                    }

                    _exponentSizes[k] = j;
                }
            }

            _coefsEnd[k] = (_frameLen - (_frameLen * 9 / 100)) >> k;
            _highBandStart[k] = (int)((blockLen * 2 * highFreq) / sampleRate + 0.5);

            var count = _exponentSizes[k];
            var highIndex = 0;
            var cursor = 0;
            for (var i = 0; i < count; i++)
            {
                var start = cursor;
                cursor += _exponentBands[k][i];
                var end = cursor;
                if (start < _highBandStart[k])
                {
                    start = _highBandStart[k];
                }

                if (end > _coefsEnd[k])
                {
                    end = _coefsEnd[k];
                }

                if (end > start)
                {
                    _exponentHighBands[k][highIndex++] = end - start;
                }
            }

            _exponentHighSizes[k] = highIndex;
        }
    }

    private void InitCoefVlc(int tableIndex, int slot)
    {
        var codes = WmaTables.CoefCodes[tableIndex];
        var bits = WmaTables.CoefBits[tableIndex];
        var levels = WmaTables.Levels[tableIndex];
        var n = codes.Length;

        _coefVlc[slot] = VlcTable.Build(codes, bits);
        var runTable = new ushort[n];
        var levelTable = new ushort[n];

        var position = 0;
        var level = 1;
        for (var i = 2; i < n;)
        {
            var run = levels[position++];
            for (var j = 0; j < run; j++)
            {
                runTable[i] = (ushort)j;
                levelTable[i] = (ushort)level;
                i++;
            }

            level++;
        }

        _runTable[slot] = runTable;
        _levelTable[slot] = levelTable;
    }

    private void InitLsp()
    {
        var wdel = Math.PI / _frameLen;
        for (var i = 0; i < _frameLen; i++)
        {
            _lspCosTable[i] = (float)(2.0 * Math.Cos(wdel * i));
        }

        for (var i = 0; i < 256; i++)
        {
            _lspPowETable[i] = (float)Math.Pow(2.0, (i - 126) * -0.25);
        }

        var b = 1.0;
        for (var i = (1 << LspPowBits) - 1; i >= 0; i--)
        {
            var m = (1 << LspPowBits) + i;
            var a = m * (0.5 / (1 << LspPowBits));
            a = Math.Pow(a, -0.25);
            _lspPowM1[i] = (float)((2 * a) - b);
            _lspPowM2[i] = (float)(b - a);
            b = a;
        }
    }

    private float PowM1_4(float x)
    {
        var bits = BitConverter.SingleToUInt32Bits(x);
        var e = (int)(bits >> 23);
        var m = (int)((bits >> (23 - LspPowBits)) & ((1 << LspPowBits) - 1));
        var t = BitConverter.UInt32BitsToSingle((bits << LspPowBits & ((1u << 23) - 1)) | (127u << 23));
        return _lspPowETable[e] * (_lspPowM1[m] + (_lspPowM2[m] * t));
    }
}

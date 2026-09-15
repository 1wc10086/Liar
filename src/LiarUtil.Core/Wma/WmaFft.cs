namespace LiarUtil.Core.Wma;

internal sealed class WmaFft
{
    private readonly int _nbits;
    private readonly bool _inverse;
    private readonly float[] _expRe;
    private readonly float[] _expIm;
    private readonly ushort[] _revTab;

    internal WmaFft(int nbits, bool inverse)
    {
        _nbits = nbits;
        _inverse = inverse;
        var n = 1 << nbits;
        var half = n >> 1;
        _expRe = new float[half];
        _expIm = new float[half];
        var sign = inverse ? 1.0 : -1.0;
        for (var i = 0; i < half; i++)
        {
            var alpha = 2 * Math.PI * i / n;
            _expRe[i] = (float)Math.Cos(alpha);
            _expIm[i] = (float)(Math.Sin(alpha) * sign);
        }

        _revTab = new ushort[n];
        for (var i = 0; i < n; i++)
        {
            var m = 0;
            for (var j = 0; j < nbits; j++)
            {
                m |= ((i >> j) & 1) << (nbits - j - 1);
            }

            _revTab[i] = (ushort)m;
        }
    }

    internal ushort[] RevTab => _revTab;

    internal void Calc(float[] z)
    {
        var np = 1 << _nbits;
        var expRe = _expRe;
        var expIm = _expIm;

        for (var j = 0; j < (np >> 1); j++)
        {
            var index = 4 * j;
            var p0Re = z[index];
            var p0Im = z[index + 1];
            var p1Re = z[index + 2];
            var p1Im = z[index + 3];
            z[index] = p0Re + p1Re;
            z[index + 1] = p0Im + p1Im;
            z[index + 2] = p0Re - p1Re;
            z[index + 3] = p0Im - p1Im;
        }

        for (var j = 0; j < (np >> 2); j++)
        {
            var index = 8 * j;
            var a0Re = z[index];
            var a0Im = z[index + 1];
            var a2Re = z[index + 4];
            var a2Im = z[index + 5];
            z[index] = a0Re + a2Re;
            z[index + 1] = a0Im + a2Im;
            z[index + 4] = a0Re - a2Re;
            z[index + 5] = a0Im - a2Im;

            var b1Re = z[index + 2];
            var b1Im = z[index + 3];
            var b3Re = z[index + 6];
            var b3Im = z[index + 7];
            if (_inverse)
            {
                z[index + 2] = b1Re - b3Im;
                z[index + 3] = b1Im + b3Re;
                z[index + 6] = b1Re + b3Im;
                z[index + 7] = b1Im - b3Re;
            }
            else
            {
                z[index + 2] = b1Re + b3Im;
                z[index + 3] = b1Im - b3Re;
                z[index + 6] = b1Re - b3Im;
                z[index + 7] = b1Im + b3Re;
            }
        }

        var nblocks = np >> 3;
        var nloops = 1 << 2;
        var np2 = np >> 1;
        while (nblocks != 0)
        {
            var p = 0;
            var q = nloops;
            for (var j = 0; j < nblocks; j++)
            {
                var pRe = z[2 * p];
                var pIm = z[2 * p + 1];
                var qRe = z[2 * q];
                var qIm = z[2 * q + 1];
                z[2 * p] = pRe + qRe;
                z[2 * p + 1] = pIm + qIm;
                z[2 * q] = pRe - qRe;
                z[2 * q + 1] = pIm - qIm;
                p++;
                q++;

                for (var l = nblocks; l < np2; l += nblocks)
                {
                    var er = expRe[l];
                    var ei = expIm[l];
                    var xr = z[2 * q];
                    var xi = z[2 * q + 1];
                    var tRe = er * xr - ei * xi;
                    var tIm = er * xi + ei * xr;
                    pRe = z[2 * p];
                    pIm = z[2 * p + 1];
                    z[2 * p] = pRe + tRe;
                    z[2 * p + 1] = pIm + tIm;
                    z[2 * q] = pRe - tRe;
                    z[2 * q + 1] = pIm - tIm;
                    p++;
                    q++;
                }

                p += nloops;
                q += nloops;
            }

            nblocks >>= 1;
            nloops <<= 1;
        }
    }
}

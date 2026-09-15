namespace LiarUtil.Core.Wma;

internal sealed class WmaMdct
{
    private readonly int _nbits;
    private readonly int _n;
    private readonly float[] _tcos;
    private readonly float[] _tsin;
    private readonly WmaFft _fft;
    private readonly float[] _scratch;

    internal WmaMdct(int nbits)
    {
        _nbits = nbits;
        _n = 1 << nbits;
        var n4 = _n >> 2;
        _tcos = new float[n4];
        _tsin = new float[n4];
        for (var i = 0; i < n4; i++)
        {
            var alpha = 2 * Math.PI * (i + 1.0 / 8.0) / _n;
            _tcos[i] = -(float)Math.Cos(alpha);
            _tsin[i] = -(float)Math.Sin(alpha);
        }

        _fft = new WmaFft(nbits - 2, true);
        _scratch = new float[2 * _n];
    }

    internal void Imdct(ReadOnlySpan<float> input, float[] output)
    {
        var n = _n;
        var n2 = n >> 1;
        var n4 = n >> 2;
        var n8 = n >> 3;
        var revTab = _fft.RevTab;
        var tcos = _tcos;
        var tsin = _tsin;
        var z = _scratch;

        Array.Clear(z, 0, 2 * n);

        var in1 = 0;
        var in2 = n2 - 1;
        for (var k = 0; k < n4; k++)
        {
            var j = revTab[k];
            var are = input[in2];
            var aim = input[in1];
            var bre = tcos[k];
            var bim = tsin[k];
            z[2 * j] = are * bre - aim * bim;
            z[2 * j + 1] = are * bim + aim * bre;
            in1 += 2;
            in2 -= 2;
        }

        _fft.Calc(z);

        for (var k = 0; k < n4; k++)
        {
            var are = z[2 * k];
            var aim = z[2 * k + 1];
            var bre = tcos[k];
            var bim = tsin[k];
            z[2 * k] = are * bre - aim * bim;
            z[2 * k + 1] = are * bim + aim * bre;
        }

        for (var k = 0; k < n8; k++)
        {
            var imA = z[2 * (n8 + k) + 1];
            output[2 * k] = -imA;
            output[n2 - 1 - 2 * k] = imA;

            var reB = z[2 * (n8 - 1 - k)];
            output[2 * k + 1] = reB;
            output[n2 - 1 - 2 * k - 1] = -reB;

            var reC = z[2 * (k + n8)];
            output[n2 + 2 * k] = -reC;
            output[n - 1 - 2 * k] = -reC;

            var imD = z[2 * (n8 - k - 1) + 1];
            output[n2 + 2 * k + 1] = imD;
            output[n - 2 - 2 * k] = imD;
        }
    }
}

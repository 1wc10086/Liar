using System.Globalization;

namespace LiarUtil.Core.Rton.Json;

internal static class YyjsonNumberFormatter
{
    public static string FormatReal(double value)
    {
        if (value == 0.0)
        {
            return double.IsNegative(value) ? "-0.0" : "0.0";
        }

        var rendered = value.ToString("R", CultureInfo.InvariantCulture);
        var negative = rendered[0] == '-';
        var body = negative ? rendered[1..] : rendered;
        var (digits, exponent) = Decompose(body);
        var decimalOffset = exponent + 1;

        string result;
        if (decimalOffset is > -6 and <= 21)
        {
            if (decimalOffset > 0)
            {
                var integerPart = decimalOffset <= digits.Length
                    ? digits[..decimalOffset]
                    : digits + new string('0', decimalOffset - digits.Length);
                var fraction = decimalOffset < digits.Length ? digits[decimalOffset..].TrimEnd('0') : "";
                result = integerPart + "." + (fraction.Length == 0 ? "0" : fraction);
            }
            else
            {
                result = "0." + new string('0', -decimalOffset) + digits;
            }
        }
        else
        {
            var fraction = digits.Length > 1 ? digits[1..].TrimEnd('0') : "";
            result = digits[0] + (fraction.Length > 0 ? "." + fraction : "") + "e" + exponent;
        }

        return negative ? "-" + result : result;
    }

    private static (string Digits, int Exponent) Decompose(string body)
    {
        var exponentIndex = body.IndexOf('E');
        if (exponentIndex >= 0)
        {
            var mantissa = body[..exponentIndex];
            var exponent = int.Parse(body[(exponentIndex + 1)..], CultureInfo.InvariantCulture);
            var dot = mantissa.IndexOf('.');
            var integerPart = dot < 0 ? mantissa : mantissa[..dot];
            var fraction = dot < 0 ? "" : mantissa[(dot + 1)..];
            return (integerPart + fraction, exponent + integerPart.Length - 1);
        }

        var decimalDot = body.IndexOf('.');
        if (decimalDot < 0)
        {
            return (body, body.Length - 1);
        }

        var integer = body[..decimalDot];
        var fractional = body[(decimalDot + 1)..];
        if (integer is "0" or "")
        {
            var leadingZeros = 0;
            while (leadingZeros < fractional.Length && fractional[leadingZeros] == '0')
            {
                leadingZeros++;
            }

            var digits = fractional[leadingZeros..];
            return digits.Length == 0 ? ("0", 0) : (digits, -(leadingZeros + 1));
        }

        return (integer + fractional, integer.Length - 1);
    }
}

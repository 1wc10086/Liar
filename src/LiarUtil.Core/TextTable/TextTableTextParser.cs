using System.Buffers;

namespace LiarUtil.Core.TextTable;

internal static class TextTableTextParser
{
    private static readonly SearchValues<char> LineTerminators = SearchValues.Create("\n\r\u2028\u2029");

    public static Dictionary<string, string> Parse(string text)
    {
        var values = new Dictionary<string, string>();
        var length = text.Length;
        var index = 0;
        var lastKeyEnd = -1;
        while (TryReadKey(text, index, out var keyStart, out var keyEnd, out var matchEnd))
        {
            var content = text[keyStart..keyEnd];
            var start = matchEnd + 1;
            var stop = start > length ? null : FindValueStop(text, start);
            string value;
            int next;
            if (stop is null)
            {
                value = "";
                next = length;
            }
            else
            {
                value = text[start..stop.Value];
                next = stop.Value;
            }

            values[content] = value;
            if (lastKeyEnd >= 0 && next <= lastKeyEnd && matchEnd <= lastKeyEnd)
            {
                break;
            }

            lastKeyEnd = matchEnd;
            index = next <= index ? index + 1 : next;
        }

        return values;
    }

    private static bool TryReadKey(string text, int start, out int keyStart, out int keyEnd, out int matchEnd)
    {
        keyStart = 0;
        keyEnd = 0;
        matchEnd = 0;
        var length = text.Length;
        for (var index = start; index < length; index++)
        {
            if (text[index] != '[')
            {
                continue;
            }

            if (index > 0 && !LineTerminators.Contains(text[index - 1]))
            {
                continue;
            }

            var end = index + 1;
            while (end < length && !LineTerminators.Contains(text[end]))
            {
                end++;
            }

            if (end - index >= 3 && text[end - 1] == ']')
            {
                keyStart = index + 1;
                keyEnd = end - 1;
                matchEnd = end;
                return true;
            }
        }

        return false;
    }

    private static int? FindValueStop(string text, int start)
    {
        var length = text.Length;
        for (var position = start; position <= length; position++)
        {
            var probe = position;
            while (probe < length && (text[probe] == '\n' || text[probe] == '\r'))
            {
                probe++;
            }

            if (probe < length && text[probe] == '[')
            {
                return position;
            }

            if (probe == length || (probe == length - 1 && text[probe] == '\n'))
            {
                return position;
            }
        }

        return null;
    }
}

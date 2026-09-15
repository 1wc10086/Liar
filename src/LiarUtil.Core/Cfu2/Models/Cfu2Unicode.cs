using System.Text.Json.Serialization;
using LiarUtil.Core.Cfu2.Json;

namespace LiarUtil.Core.Cfu2.Models;

[JsonConverter(typeof(Cfu2UnicodeConverter))]
public readonly record struct Cfu2Unicode(ushort Value)
{
    public static implicit operator Cfu2Unicode(ushort value) => new(value);

    public static implicit operator ushort(Cfu2Unicode value) => value.Value;

    public override string ToString() => Value is >= 0x20 and <= 0x7E ? ((char)Value).ToString() : $"U+{Value:X4}";
}

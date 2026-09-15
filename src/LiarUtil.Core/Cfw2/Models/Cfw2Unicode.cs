using System.Text.Json.Serialization;
using LiarUtil.Core.Cfw2.Json;

namespace LiarUtil.Core.Cfw2.Models;

[JsonConverter(typeof(Cfw2UnicodeConverter))]
public readonly record struct Cfw2Unicode(ushort Value)
{
    public static implicit operator Cfw2Unicode(ushort value) => new(value);

    public static implicit operator ushort(Cfw2Unicode value) => value.Value;

    public override string ToString() => Value is >= 0x20 and <= 0x7E ? ((char)Value).ToString() : $"U+{Value:X4}";
}

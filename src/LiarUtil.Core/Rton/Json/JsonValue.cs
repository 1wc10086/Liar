namespace LiarUtil.Core.Rton.Json;

public abstract class JsonValue;

public sealed class JsonObject : JsonValue
{
    public List<(byte[] Key, JsonValue Value)> Entries { get; } = [];
}

public sealed class JsonArray : JsonValue
{
    public List<JsonValue> Items { get; } = [];
}

public sealed class JsonString : JsonValue
{
    public byte[] Bytes { get; }

    public JsonString(ReadOnlySpan<byte> bytes) => Bytes = bytes.ToArray();
}

public enum NumberKind
{
    Signed,
    Unsigned,
    Real,
}

public sealed class JsonNumber : JsonValue
{
    public NumberKind Kind { get; }
    public long Signed { get; }
    public ulong Unsigned { get; }
    public double Real { get; }

    public JsonNumber(long value)
    {
        Kind = NumberKind.Signed;
        Signed = value;
    }

    public JsonNumber(ulong value)
    {
        Kind = NumberKind.Unsigned;
        Unsigned = value;
    }

    public JsonNumber(double value)
    {
        Kind = NumberKind.Real;
        Real = value;
    }
}

public sealed class JsonBool : JsonValue
{
    public bool Value { get; }

    public JsonBool(bool value) => Value = value;
}

public sealed class JsonNull : JsonValue;

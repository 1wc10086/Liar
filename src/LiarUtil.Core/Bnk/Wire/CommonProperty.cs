namespace LiarUtil.Core.Bnk.Model;

internal enum CommonValueKind
{
    Boolean,
    Integer,
    Floater,
    Enumerated,
    Identifier,
}

internal struct CommonPropertyValue
{
    public CommonValueKind Kind;
    public bool Bool;
    public long Int;
    public double Float;
    public byte Enum;
    public long Identifier;
}

internal sealed class CommonPropertyEntry<T> where T : struct
{
    public T Key;
    public CommonPropertyValue First;
    public CommonPropertyValue Second;
}

internal sealed class CommonPropertyMap<T> where T : struct
{
    public List<CommonPropertyEntry<T>> Regular = [];
    public List<CommonPropertyEntry<T>> Randomizer = [];
}

internal interface ICommonPropertyTable<T> where T : struct
{
    T KeyOf(BankVersion version, int raw);

    int RawOf(BankVersion version, T key);

    CommonValueKind KindOf(BankVersion version, T key);

    CommonPropertyValue DefaultOf(BankVersion version, T key);
}

internal static class CommonProperty
{
    public static void Read<T>(BankContext context, CommonPropertyMap<T> map, ICommonPropertyTable<T> table, bool randomizable) where T : struct
    {
        if (context.Reading)
        {
            ReadEntries(context, map.Regular, table, 1);
            if (randomizable)
            {
                ReadEntries(context, map.Randomizer, table, 2);
            }
        }
        else
        {
            WriteEntries(context, map.Regular, table, 1);
            if (randomizable)
            {
                WriteEntries(context, map.Randomizer, table, 2);
            }
        }
    }

    private static void WriteEntries<T>(BankContext context, List<CommonPropertyEntry<T>> list, ICommonPropertyTable<T> table, int valueCount) where T : struct
    {
        var kept = new List<CommonPropertyEntry<T>>(list.Count);
        foreach (var entry in list)
        {
            var fallback = table.DefaultOf(context.Version, entry.Key);
            if (valueCount > 1)
            {
                if (EqualsDefault(entry.First, fallback) && EqualsDefault(entry.Second, fallback))
                {
                    continue;
                }
            }
            else if (EqualsDefault(entry.First, fallback))
            {
                continue;
            }
            kept.Add(entry);
        }
        context.WriteSize(SizeKind.U8, (ulong)kept.Count);
        foreach (var entry in kept)
        {
            context.WriteSize(SizeKind.U8, (ulong)table.RawOf(context.Version, entry.Key));
            WriteValue(context, entry.First);
            if (valueCount > 1)
            {
                WriteValue(context, entry.Second);
            }
        }
    }

    private static bool EqualsDefault(CommonPropertyValue value, CommonPropertyValue fallback)
    {
        if (value.Kind != fallback.Kind)
        {
            return false;
        }
        return value.Kind switch
        {
            CommonValueKind.Boolean => value.Bool == fallback.Bool,
            CommonValueKind.Integer => value.Int == fallback.Int,
            CommonValueKind.Floater => value.Float == fallback.Float,
            CommonValueKind.Enumerated => value.Enum == fallback.Enum,
            CommonValueKind.Identifier => value.Identifier == fallback.Identifier,
            _ => false,
        };
    }

    private static void ReadEntries<T>(BankContext context, List<CommonPropertyEntry<T>> list, ICommonPropertyTable<T> table, int valueCount) where T : struct
    {
        if (context.Reading)
        {
            var count = context.Reader.ReadUInt8();
            list.Clear();
            for (var i = 0; i < count; i++)
            {
                var entry = new CommonPropertyEntry<T>();
                entry.Key = table.KeyOf(context.Version, context.Reader.ReadUInt8());
                for (var k = 0; k < valueCount; k++)
                {
                    var value = ReadValue(context, table.KindOf(context.Version, entry.Key));
                    if (k == 0)
                    {
                        entry.First = value;
                    }
                    else
                    {
                        entry.Second = value;
                    }
                }
                list.Add(entry);
            }
        }
    }

    private static CommonPropertyValue ReadValue(BankContext context, CommonValueKind kind)
    {
        var value = new CommonPropertyValue { Kind = kind };
        switch (kind)
        {
            case CommonValueKind.Boolean:
            {
                var bits = context.Bits32();
                bits.Bit(ref value.Bool);
                bits.Done();
                break;
            }
            case CommonValueKind.Integer:
            {
                var raw = 0L;
                context.S32(ref raw);
                value.Int = raw;
                break;
            }
            case CommonValueKind.Floater:
            {
                var raw = 0d;
                context.F32(ref raw);
                value.Float = raw;
                break;
            }
            case CommonValueKind.Enumerated:
            {
                var raw = (byte)0;
                context.En32(ref raw);
                value.Enum = raw;
                break;
            }
            case CommonValueKind.Identifier:
            {
                var raw = 0L;
                context.Id(ref raw);
                value.Identifier = raw;
                break;
            }
        }
        return value;
    }

    private static void WriteValue(BankContext context, CommonPropertyValue value)
    {
        switch (value.Kind)
        {
            case CommonValueKind.Boolean:
            {
                var bits = context.Bits32();
                bits.Bit(ref value.Bool);
                bits.Done();
                break;
            }
            case CommonValueKind.Integer:
            {
                var raw = value.Int;
                context.S32(ref raw);
                break;
            }
            case CommonValueKind.Floater:
            {
                var raw = value.Float;
                context.F32(ref raw);
                break;
            }
            case CommonValueKind.Enumerated:
            {
                var raw = value.Enum;
                context.En32(ref raw);
                break;
            }
            case CommonValueKind.Identifier:
            {
                var raw = value.Identifier;
                context.Id(ref raw);
                break;
            }
        }
    }

    public static void AddBool<T>(CommonPropertyMap<T> map, T key, bool value) where T : struct
    {
        map.Regular.Add(new CommonPropertyEntry<T> { Key = key, First = new CommonPropertyValue { Kind = CommonValueKind.Boolean, Bool = value } });
    }

    public static void AddEnum<T>(CommonPropertyMap<T> map, T key, byte value) where T : struct
    {
        map.Regular.Add(new CommonPropertyEntry<T> { Key = key, First = new CommonPropertyValue { Kind = CommonValueKind.Enumerated, Enum = value } });
    }

    public static void AddIdentifier<T>(CommonPropertyMap<T> map, T key, long value) where T : struct
    {
        map.Regular.Add(new CommonPropertyEntry<T> { Key = key, First = new CommonPropertyValue { Kind = CommonValueKind.Identifier, Identifier = value } });
    }

    public static void AddInt<T>(CommonPropertyMap<T> map, T key, long value) where T : struct
    {
        map.Regular.Add(new CommonPropertyEntry<T> { Key = key, First = new CommonPropertyValue { Kind = CommonValueKind.Integer, Int = value } });
    }

    public static void AddFloat<T>(CommonPropertyMap<T> map, T key, double value) where T : struct
    {
        map.Regular.Add(new CommonPropertyEntry<T> { Key = key, First = new CommonPropertyValue { Kind = CommonValueKind.Floater, Float = value } });
    }

    public static void AddRandomizableInt<T>(CommonPropertyMap<T> map, T key, RandomizableValue<long> value) where T : struct
    {
        map.Regular.Add(new CommonPropertyEntry<T> { Key = key, First = new CommonPropertyValue { Kind = CommonValueKind.Integer, Int = value.Value } });
        map.Randomizer.Add(new CommonPropertyEntry<T>
        {
            Key = key,
            First = new CommonPropertyValue { Kind = CommonValueKind.Integer, Int = value.MinimumValue },
            Second = new CommonPropertyValue { Kind = CommonValueKind.Integer, Int = value.MaximumValue },
        });
    }


    public static void ExchangeRandomizableFloat<T>(BankContext context, CommonPropertyMap<T> map, T key, RandomizableValue<double> value, double fallback) where T : struct
    {
        if (context.Reading)
        {
            RandomizableFloat(map, key, value, fallback);
        }
        else
        {
            AddRandomizableFloat(map, key, value);
        }
    }

    public static void ExchangeRandomizableInt<T>(BankContext context, CommonPropertyMap<T> map, T key, RandomizableValue<long> value, long fallback) where T : struct
    {
        if (context.Reading)
        {
            RandomizableInt(map, key, value, fallback);
        }
        else
        {
            AddRandomizableInt(map, key, value);
        }
    }

    public static void ExchangeRegularFloat<T>(BankContext context, CommonPropertyMap<T> map, T key, RegularValue<double> value, double fallback) where T : struct
    {
        if (context.Reading)
        {
            value.Value = Float(map, key, fallback);
        }
        else
        {
            AddFloat(map, key, value.Value);
        }
    }
    public static void AddRandomizableFloat<T>(CommonPropertyMap<T> map, T key, RandomizableValue<double> value) where T : struct
    {
        map.Regular.Add(new CommonPropertyEntry<T> { Key = key, First = new CommonPropertyValue { Kind = CommonValueKind.Floater, Float = value.Value } });
        map.Randomizer.Add(new CommonPropertyEntry<T>
        {
            Key = key,
            First = new CommonPropertyValue { Kind = CommonValueKind.Floater, Float = value.MinimumValue },
            Second = new CommonPropertyValue { Kind = CommonValueKind.Floater, Float = value.MaximumValue },
        });
    }

    public static bool Bool<T>(CommonPropertyMap<T> map, T key, bool fallback) where T : struct
    {
        var value = Find(map, key);
        return value.HasValue && value.Value.Kind == CommonValueKind.Boolean ? value.Value.Bool : fallback;
    }

    public static long Int<T>(CommonPropertyMap<T> map, T key, long fallback) where T : struct
    {
        var value = Find(map, key);
        return value.HasValue && value.Value.Kind == CommonValueKind.Integer ? value.Value.Int : fallback;
    }

    public static double Float<T>(CommonPropertyMap<T> map, T key, double fallback) where T : struct
    {
        var value = Find(map, key);
        return value.HasValue && value.Value.Kind == CommonValueKind.Floater ? value.Value.Float : fallback;
    }

    public static byte Enum<T>(CommonPropertyMap<T> map, T key, byte fallback) where T : struct
    {
        var value = Find(map, key);
        return value.HasValue && value.Value.Kind == CommonValueKind.Enumerated ? value.Value.Enum : fallback;
    }

    public static long Identifier<T>(CommonPropertyMap<T> map, T key, long fallback) where T : struct
    {
        var value = Find(map, key);
        return value.HasValue && value.Value.Kind == CommonValueKind.Identifier ? value.Value.Identifier : fallback;
    }

    public static void RandomizableInt<T>(CommonPropertyMap<T> map, T key, RandomizableValue<long> value, long fallback) where T : struct
    {
        var first = Find(map, key);
        value.Value = first.HasValue && first.Value.Kind == CommonValueKind.Integer ? first.Value.Int : fallback;
        var pair = FindRandomizer(map, key);
        if (pair.HasValue)
        {
            value.MinimumValue = pair.Value.Item1.Kind == CommonValueKind.Integer ? pair.Value.Item1.Int : 0L;
            value.MaximumValue = pair.Value.Item2.Kind == CommonValueKind.Integer ? pair.Value.Item2.Int : 0L;
        }
        else
        {
            value.MinimumValue = 0L;
            value.MaximumValue = 0L;
        }
    }

    public static void RandomizableFloat<T>(CommonPropertyMap<T> map, T key, RandomizableValue<double> value, double fallback) where T : struct
    {
        var first = Find(map, key);
        value.Value = first.HasValue && first.Value.Kind == CommonValueKind.Floater ? first.Value.Float : fallback;
        var pair = FindRandomizer(map, key);
        if (pair.HasValue)
        {
            value.MinimumValue = pair.Value.Item1.Kind == CommonValueKind.Floater ? pair.Value.Item1.Float : 0d;
            value.MaximumValue = pair.Value.Item2.Kind == CommonValueKind.Floater ? pair.Value.Item2.Float : 0d;
        }
        else
        {
            value.MinimumValue = 0d;
            value.MaximumValue = 0d;
        }
    }

    private static (CommonPropertyValue Item1, CommonPropertyValue Item2)? FindRandomizer<T>(CommonPropertyMap<T> map, T key) where T : struct
    {
        foreach (var entry in map.Randomizer)
        {
            if (entry.Key.Equals(key))
            {
                return (entry.First, entry.Second);
            }
        }
        return null;
    }

    private static CommonPropertyValue? Find<T>(CommonPropertyMap<T> map, T key) where T : struct
    {
        foreach (var entry in map.Regular)
        {
            if (entry.Key.Equals(key))
            {
                return entry.First;
            }
        }
        return null;
    }
}

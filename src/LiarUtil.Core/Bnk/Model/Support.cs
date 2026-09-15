namespace LiarUtil.Core.Bnk.Model;

internal interface IWireEnum<T> where T : struct
{
    int Width(BankVersion version);
    T FromRaw(BankVersion version, int raw);
    int ToRaw(BankVersion version, T value);
}

internal struct Position2<TX>
{
    public TX X;
    public TX Y;
}

internal struct Position2<TX, TY>
{
    public TX X;
    public TY Y;
}

internal struct Position3<TX>
{
    public TX X;
    public TX Y;
    public TX Z;
}

internal struct WireTuple<T0, T1, T2, T3, T4>
{
    public T0 Item1;
    public T1 Item2;
    public T2 Item3;
    public T3 Item4;
    public T4 Item5;
}

internal sealed class RegularValue<T>
{
    public T Value = default!;
}

internal sealed class RandomizableValue<T>
{
    public T Value = default!;
    public T MinimumValue = default!;
    public T MaximumValue = default!;
}

internal sealed class Hierarchy
{
    public HierarchyType Type = HierarchyType.Unknown;
    public object? Item;
}

internal sealed class EventActionPropertyItem
{
    public EventActionPropertyType Type = EventActionPropertyType.PlayAudio;
    public object? Item;
}

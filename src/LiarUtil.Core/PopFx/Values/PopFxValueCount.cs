using LiarUtil.Core.PopFx.Models;

namespace LiarUtil.Core.PopFx.Values;

internal static class PopFxValueCount
{
    public static int Of(PopFxValueType type) => type switch
    {
        PopFxValueType.Float or PopFxValueType.Int or PopFxValueType.String => 1,
        PopFxValueType.Float2 or PopFxValueType.Int2 => 2,
        PopFxValueType.Float3 or PopFxValueType.Int3 => 3,
        PopFxValueType.Float4 or PopFxValueType.Int4 => 4,
        _ => 1,
    };
}

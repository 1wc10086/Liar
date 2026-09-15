namespace LiarUtil.Core.PopFx.Binary;

internal static class PopFxLayout
{
    public const uint TechniqueSize = 24;
    public const uint AnnotationSize = 8;
    public const uint AnnotationSizeV1 = 12;
    public const uint StringSize = 12;
    public const uint ValueSize = 20;
    public const uint PassSize = 28;
    public const uint SettingSize = 20;
    public const uint ShaderParameterSize = 8;
    public const uint ShaderSize = 12;
    public const uint ShaderSizeV3 = 20;
    public const uint EmptyIndex = uint.MaxValue;

    public static int SectionCount(PopFxVariant variant) => variant == PopFxVariant.V3 ? 8 : 7;

    public static uint HeaderSize(PopFxVariant variant) => (uint)(8 + SectionCount(variant) * 12 + 4);

    public static uint AnnotationSizeFor(PopFxVariant variant) => variant == PopFxVariant.V1 ? AnnotationSizeV1 : AnnotationSize;

    public static uint ShaderSizeFor(PopFxVariant variant) => variant == PopFxVariant.V3 ? ShaderSizeV3 : ShaderSize;

    public static bool HasShaderParameters(PopFxVariant variant) => variant == PopFxVariant.V3;

    public static uint SectionSize(PopFxVariant variant, int index)
    {
        if (variant == PopFxVariant.V3)
        {
            return index switch
            {
                0 => TechniqueSize,
                1 => AnnotationSize,
                2 => StringSize,
                3 => ValueSize,
                4 => PassSize,
                5 => SettingSize,
                6 => ShaderParameterSize,
                7 => ShaderSizeV3,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };
        }
        return index switch
        {
            0 => TechniqueSize,
            1 => variant == PopFxVariant.V1 ? AnnotationSizeV1 : AnnotationSize,
            2 => StringSize,
            3 => ValueSize,
            4 => PassSize,
            5 => SettingSize,
            6 => ShaderSize,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };
    }
}

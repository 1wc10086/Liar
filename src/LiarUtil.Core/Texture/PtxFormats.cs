namespace LiarUtil.Core.Texture;

internal enum PtxTag
{
    Argb8888,
    Abgr8888,
    Rgba4444,
    Rgb565,
    Rgba5551,
    Rgba4444Block,
    Rgb565Block,
    Rgba5551Block,
    Xrgb8888AuxAlpha,
    Xbgr8888AuxAlpha,
    Argb8888Be,
    Argb8888PaddingBe,
    Dxt1Rgb,
    Dxt3Rgba,
    Dxt5Rgba,
    Dxt5RgbaMortonBlock,
    Dxt5RgbaBe,
    Etc1Rgb,
    Pvrtc4BppRgba,
    Pvrtc2BppRgba,
    Astc4X4,
    Astc5X5,
    Astc6X6,
    Astc8X8,
}

internal readonly record struct PtxFormatSpec(PtxTag Tag, uint FileId, PtxAlphaScheme Alpha, bool BigEndianHeader, bool MarkAlpha64, bool Valid);

internal static class PtxFormats
{
    private const uint PtMagic = 0x70747831;

    private static PtxFormatSpec Simple(PtxTag tag, uint id, bool bigEndian = false, bool mark64 = false) =>
        new(tag, id, PtxAlphaScheme.None, bigEndian, mark64, true);

    private static PtxFormatSpec Aux(PtxTag tag, uint id, PtxAlphaScheme alpha, bool mark64 = false) =>
        new(tag, id, alpha, false, mark64, true);

    public static bool IsAuxFileId(uint id) => id is >= 147 and <= 150;

    public static bool IsAstcFileId(uint id) => id is >= 160 and <= 163;

    public static PtxFormatSpec Parse(string format)
    {
        var spec = format switch
        {
            "ARGB8888" => Simple(PtxTag.Argb8888, 0),
            "ABGR8888" => Simple(PtxTag.Abgr8888, 0),
            "RGBA4444" => Simple(PtxTag.Rgba4444, 1),
            "RGB565" => Simple(PtxTag.Rgb565, 2),
            "RGBA5551" => Simple(PtxTag.Rgba5551, 3),
            "RGBA4444_Block" => Simple(PtxTag.Rgba4444Block, 21),
            "RGB565_Block" => Simple(PtxTag.Rgb565Block, 22),
            "RGBA5551_Block" => Simple(PtxTag.Rgba5551Block, 23),
            "XRGB8888_A8" => Aux(PtxTag.Xrgb8888AuxAlpha, 149, PtxAlphaScheme.A8),
            "XBGR8888_A8" => Aux(PtxTag.Xbgr8888AuxAlpha, 149, PtxAlphaScheme.A8),
            "XRGB8888_A_Palette" => Aux(PtxTag.Xrgb8888AuxAlpha, 149, PtxAlphaScheme.Palette4_A1, true),
            "XBGR8888_A_Palette" => Aux(PtxTag.Xbgr8888AuxAlpha, 149, PtxAlphaScheme.Palette4_A1, true),
            "ARGB8888_BE" or "ARGB8888(BE)" => Simple(PtxTag.Argb8888Be, 0, true),
            "ARGB8888_Padding_BE" or "ARGB8888_Padding(BE)" => Simple(PtxTag.Argb8888PaddingBe, 0, true),
            "DXT1_RGB" => Simple(PtxTag.Dxt1Rgb, 35),
            "DXT3_RGBA" => Simple(PtxTag.Dxt3Rgba, 36),
            "DXT5_RGBA" => Simple(PtxTag.Dxt5Rgba, 37),
            "DXT5_RGBA_MortonBlock" => Simple(PtxTag.Dxt5RgbaMortonBlock, 5),
            "DXT5_RGBA_BE" => Simple(PtxTag.Dxt5RgbaBe, 5, true),
            "ETC1_RGB" => Simple(PtxTag.Etc1Rgb, 32),
            "ETC1_RGB_A8" => Aux(PtxTag.Etc1Rgb, 147, PtxAlphaScheme.A8),
            "ETC1_RGB_A_Palette" => Aux(PtxTag.Etc1Rgb, 147, PtxAlphaScheme.Palette4_A1, true),
            "PVRTC_4BPP_RGBA" => Simple(PtxTag.Pvrtc4BppRgba, 30),
            "PVRTC_4BPP_RGBA_A8" => Aux(PtxTag.Pvrtc4BppRgba, 148, PtxAlphaScheme.A8),
            "PVRTC_4BPP_RGBA_A_Palette" => Aux(PtxTag.Pvrtc4BppRgba, 148, PtxAlphaScheme.Palette4_A1, true),
            "PVRTC_2BPP_RGBA" => Simple(PtxTag.Pvrtc2BppRgba, 31),
            "ASTC_4x4" => Simple(PtxTag.Astc4X4, 160, false, true),
            "ASTC_5x5" => Simple(PtxTag.Astc5X5, 161, false, true),
            "ASTC_6x6" => Simple(PtxTag.Astc6X6, 162, false, true),
            "ASTC_8x8" => Simple(PtxTag.Astc8X8, 163, false, true),
            _ => default,
        };

        if (spec.Valid)
        {
            return spec;
        }

        if (format.Length == 0 || !int.TryParse(format, out var id))
        {
            return default;
        }

        return id switch
        {
            0 => Simple(PtxTag.Argb8888, 0),
            1 => Simple(PtxTag.Rgba4444, 1),
            2 => Simple(PtxTag.Rgb565, 2),
            3 => Simple(PtxTag.Rgba5551, 3),
            5 => Simple(PtxTag.Dxt5RgbaMortonBlock, 5),
            21 => Simple(PtxTag.Rgba4444Block, 21),
            22 => Simple(PtxTag.Rgb565Block, 22),
            23 => Simple(PtxTag.Rgba5551Block, 23),
            30 => Simple(PtxTag.Pvrtc4BppRgba, 30),
            31 => Simple(PtxTag.Pvrtc2BppRgba, 31),
            32 => Simple(PtxTag.Etc1Rgb, 32),
            35 => Simple(PtxTag.Dxt1Rgb, 35),
            36 => Simple(PtxTag.Dxt3Rgba, 36),
            37 => Simple(PtxTag.Dxt5Rgba, 37),
            147 => Aux(PtxTag.Etc1Rgb, 147, PtxAlphaScheme.A8),
            148 => Aux(PtxTag.Pvrtc4BppRgba, 148, PtxAlphaScheme.A8),
            149 => Aux(PtxTag.Xrgb8888AuxAlpha, 149, PtxAlphaScheme.A8),
            150 => Aux(PtxTag.Etc1Rgb, 150, PtxAlphaScheme.A8),
            160 => Simple(PtxTag.Astc4X4, 160, false, true),
            161 => Simple(PtxTag.Astc5X5, 161, false, true),
            162 => Simple(PtxTag.Astc6X6, 162, false, true),
            163 => Simple(PtxTag.Astc8X8, 163, false, true),
            _ => default,
        };
    }
}

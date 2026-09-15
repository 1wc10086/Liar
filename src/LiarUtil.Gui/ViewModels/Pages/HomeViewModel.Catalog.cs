namespace LiarUtil.Gui.ViewModels.Pages;

public sealed partial class HomeViewModel
{
    private static class Catalog
    {
        private static readonly string[] PtxValidFormats =
        [
            "ARGB8888", "ABGR8888", "RGBA4444", "RGB565", "RGBA5551",
            "RGBA4444_Block", "RGB565_Block", "RGBA5551_Block",
            "XRGB8888_A8", "XBGR8888_A8", "XRGB8888_A_Palette", "XBGR8888_A_Palette",
            "ARGB8888_BE", "ARGB8888_Padding_BE",
            "DXT1_RGB", "DXT3_RGBA", "DXT5_RGBA", "DXT5_RGBA_MortonBlock", "DXT5_RGBA_BE",
            "ETC1_RGB", "ETC1_RGB_A8", "ETC1_RGB_A_Palette",
            "PVRTC_4BPP_RGBA", "PVRTC_4BPP_RGBA_A8", "PVRTC_4BPP_RGBA_A_Palette", "PVRTC_2BPP_RGBA",
            "ASTC_4x4", "ASTC_5x5", "ASTC_6x6", "ASTC_8x8",
        ];

        public static readonly string[] TextureModes =
        [
            "PTX(rsb)", "cdat(Android,iOS)", "tex(ios)", "txz(Android.iOS)", "tex(TV)",
            "ptx(Xbox360)", "ptx(PS3)", "ptx(PSV)", "xnb(Windows Phone)",
        ];

        public static readonly string[] RsbVersions = ["1", "3", "4"];

        public static readonly string[] BnkVersions =
        [
            "72", "88", "112", "113", "118", "120", "125", "128", "132", "134", "135", "140", "145", "150",
        ];

        public static readonly string[] TextTableVersions = ["text", "json_map", "json_list"];

        public static readonly string[] AtlasFormats =
        [
            "RESOURCES.XML(Rsb)", "resources.xml(Old)", "resources.xml(Ancient)", "plist(Free)",
            "atlasimagemap.dat", "xml(TV)", "RESOURCES.JSON(Rsb)",
        ];

        public static readonly string[] PopFxNumbers = ["1"];

        public static readonly string[] PopFxVariants = ["1", "2", "3"];

        public static readonly string[] PpfVersions = ["1"];

        public static readonly string[] RsbPatchNumbers = ["1"];

        public static readonly string[] ParticlePlatforms =
        [
            "PC_Compiled", "Phone32_Compiled", "Phone64_Compiled", "WP_Xnb",
            "GameConsole_Compiled", "TV_Compiled",
        ];

        public static readonly string[] TrailPlatforms = ParticlePlatforms;

        public static readonly string[] ReanimPlatforms =
        [
            "PC_Compiled", "Phone32_Compiled", "Phone64_Compiled", "WP_Xnb",
            "GameConsole_Compiled", "TV_Compiled", "Flash_Xfl",
        ];

        public static readonly string[][] TextureFormatsByMode =
        [
            PtxValidFormats,
            ["CDAT"],
            ["ABGR8888", "RGBA4444", "RGBA5551", "RGB565"],
            ["ABGR8888", "RGBA4444", "RGBA5551", "RGB565"],
            ["L8", "ARGB8888", "ARGB4444", "ARGB1555", "RGB565", "ABGR8888", "RGBA4444", "RGBA5551", "XRGB8888", "LA88"],
            ["DXT5"],
            ["DXT5"],
            ["DXT5"],
            ["ABGR8888"],
        ];
    }
}

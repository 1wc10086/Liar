namespace LiarUtil.Cli;

internal sealed record FunctionEntry(string Name, string[] Modes, bool DecodeEncodeOnly = false)
{
    public bool HasSingleMode => Modes.Length == 1;
}

internal static class FunctionCatalog
{
    public const int RtonFunction = 0;
    public const int TextureFunction = 1;
    public const int RsbFunction = 2;
    public const int BnkFunction = 3;
    public const int PamFunction = 4;
    public const int PpfFunction = 5;
    public const int PopFxFunction = 6;
    public const int Cfw2Function = 7;
    public const int ParticleFunction = 8;
    public const int TrailFunction = 9;
    public const int ReanimFunction = 10;
    public const int DzFunction = 11;
    public const int PakFunction = 12;
    public const int ArcvFunction = 13;
    public const int XprFunction = 14;
    public const int AtlasFunction = 15;
    public const int NewtonFunction = 16;
    public const int RsbPatchFunction = 17;
    public const int FontWidgetDatFunction = 18;
    public const int PaxFunction = 19;
    public const int WemFunction = 20;
    public const int TextTableFunction = 21;
    public const int SpsFunction = 22;
    public const int SnrFunction = 23;
    public const int CafFunction = 24;
    public const int WmaFunction = 25;
    public const int XmFunction = 26;
    public const int XmaFunction = 27;
    public const int XnbAudioFunction = 28;
    public const int XnbFontFunction = 29;
    public const int Cfu2Function = 30;

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

    public static readonly string[] PamVersions = ["1", "2", "3", "4", "5", "6"];

    public static readonly string[] PpfVersions = ["1"];

    public static readonly string[] PopFxNumbers = ["1"];

    public static readonly string[] PopFxVariants = ["1", "2", "3"];

    public static readonly string[] TextTableVersions = ["text", "json_map", "json_list"];

    public static readonly string[] AtlasFormats =
    [
        "RESOURCES.XML(Rsb)", "resources.xml(Old)", "resources.xml(Ancient)", "plist(Free)",
        "atlasimagemap.dat", "xml(TV)", "RESOURCES.JSON(Rsb)",
    ];

    public static readonly string[] RsbPatchNumbers = ["1"];

    public static readonly string[] ParticlePlatforms =
    [
        "PC_Compiled", "Phone32_Compiled", "Phone64_Compiled", "WP_Xnb",
        "GameConsole_Compiled", "TV_Compiled",
    ];

    public static readonly string[] ReanimPlatforms =
    [
        "PC_Compiled", "Phone32_Compiled", "Phone64_Compiled", "WP_Xnb",
        "GameConsole_Compiled", "TV_Compiled", "Flash_Xfl",
    ];

    public static readonly string[] RtonEncodings = ["UTF-8", "EASCII"];

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

    public static readonly FunctionEntry[] Functions =
    [
        new("Rton", ["Decode", "Encode", "Encrypt", "Decrypt"]),
        new("Texture", TextureModes),
        new("Rsb", ["Unpack", "Pack"]),
        new("Bnk", ["Decode", "Encode"]),
        new("Pam", ["Decode", "Encode", "To Flash", "From Flash"]),
        new("Ppf", ["Decode", "Encode"]),
        new("Popfx", ["Decode", "Encode"]),
        new("Cfw2", ["Decode", "Encode"]),
        new("Particle", ["Decode", "Encode"]),
        new("Trail", ["Decode", "Encode"]),
        new("Reanim", ["Decode", "Encode"]),
        new("Dz", ["Unpack", "Pack"]),
        new("Pak", ["Unpack", "Pack"]),
        new("Arcv", ["Unpack", "Pack"]),
        new("Xpr", ["Unpack", "Pack"]),
        new("Atlas", ["Split", "Merge"]),
        new("Newton", ["Decode", "Encode"]),
        new("RsbPatch", ["Decode", "Encode"]),
        new("FontWidgetDat", ["Decode", "Encode"]),
        new("Pax", ["Decode", "Encode"]),
        new("Wem", ["Decode"]),
        new("TextTable", ["Convert"]),
        new("Sps", ["Decode", "Encode"]),
        new("Snr", ["Decode", "Encode"]),
        new("Caf", ["Decode"]),
        new("Wma", ["Decode"]),
        new("Xm", ["Decode"]),
        new("Xma", ["Decode", "Encode"]),
        new("Xnb-Audio", ["Decode"]),
        new("Xnb-Fonts", ["Decode", "Encode"]),
        new("Cfu2", ["Decode", "Encode"]),
    ];

    public static bool IsInputDirectory(int function, int mode) => function switch
    {
        RsbFunction or DzFunction or PakFunction or ArcvFunction or XprFunction => mode == 1,
        PamFunction => mode == 3,
        AtlasFunction => mode == 1,
        _ => false,
    };

    public static bool IsOutputDirectory(int function, int mode) => function switch
    {
        RsbFunction or DzFunction or PakFunction or ArcvFunction or XprFunction => mode == 0,
        PamFunction => mode == 2,
        AtlasFunction => mode == 0,
        _ => false,
    };
}
